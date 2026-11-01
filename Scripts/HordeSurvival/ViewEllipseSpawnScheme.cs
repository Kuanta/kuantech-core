using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Spawns on a band just outside the screen, whatever the camera looks like. The band lives in view
    /// space: the unit circle inscribed in the viewport, scaled by MinScale..MaxScale. A point of it is picked
    /// as an angle and a radius, then <see cref="ViewSpaceMapping.UnitToGround"/> lays it onto the ground from
    /// the camera's pitch, yaw, fov and aspect ratio, so the ring comes out egg-shaped on the ground and the
    /// same settings give a similar feel at any camera angle.
    ///
    /// The direction bias is measured on screen: the world direction of the hint is turned into the angle it
    /// has on the unit circle, and the arc is taken around that. Where the screen is wide the arc covers more
    /// ground, so heading towards the camera (or away) spreads the spawns wider than heading sideways.
    /// Without a camera it falls back to a plain ring between MinDistance and MaxDistance.
    /// </summary>
    [Serializable]
    public class ViewEllipseSpawnScheme : SpawnScheme
    {
        [Header("Band (view space)")]
        [Tooltip("Inner edge of the band, as a scale of the unit circle inscribed in the viewport. 1 = touches " +
                 "the screen edges, 1.2 = 20% beyond them.")]
        public float MinScale = 1.1f;
        [Tooltip("Outer edge of the band, same unit as Min Scale.")]
        public float MaxScale = 1.5f;
        [Tooltip("Off: the scale is picked evenly between Min and Max. On: evenly per area, so the band is " +
                 "filled with the same density throughout.")]
        public bool UniformByArea = true;

        [Header("Limits (meters)")]
        [Tooltip("Never spawn closer than this to the focus point, even when the camera would allow it.")]
        public float MinDistance = 8f;
        [Tooltip("Never spawn farther than this from the focus point. With a low camera pitch the top of the " +
                 "screen is very far away (or above the horizon); a point that lands beyond this is pulled " +
                 "back along the line to the focus. Keep it below the spawner's Unseen Max Distance.")]
        public float MaxDistance = 60f;

        [Tooltip("The camera tilt (degrees from straight down) is clamped to this, so the ground is never " +
                 "stretched without limit. 45 = at most 1.41x taller than wide before perspective.")]
        [Range(0f, 85f)]
        public float MaxTilt = 45f;

        [Header("Direction Bias")]
        [Tooltip("Half-angle, on the view unit circle, of the arc used when a spawn hint asks for a direction, " +
                 "at full hint weight.")]
        [Range(5f, 180f)]
        public float SpreadAngle = 60f;

        // How far along the hint direction the focus is probed to find which way it moves on screen.
        private const float ProbeDistance = 5f;

        protected override Vector3 SampleCandidate(Vector3 focus)
        {
            return Sample(focus, SpawnHint.None);
        }

        protected override Vector3 SampleCandidate(Vector3 focus, in SpawnHint hint)
        {
            return Sample(focus, hint);
        }

        /// <summary>
        /// The ground point under a view-space point, using the given camera, kept within
        /// <see cref="MinDistance"/>..<see cref="MaxDistance"/> of the focus point (groundY = focus.y).
        /// </summary>
        public Vector3 MapToGround(UnityEngine.Camera view, Vector2 unitPoint, Vector3 focus)
        {
            Vector3 euler = view.transform.eulerAngles;
            Vector3 center = ViewSpaceMapping.ScreenCenterOnGround(view.transform.position, view.transform.forward, focus.y);
            float tilt = Mathf.Clamp(90f - euler.x, 0f, 90f);

            // Orthographic: the screen size is the camera's size and there is no perspective stage (D = 0).
            // Perspective: D is the distance to the screen-center ground point, and the screen is
            // D * tan(fov / 2) tall at that distance.
            float distance = view.orthographic ? 0f : Vector3.Distance(view.transform.position, center);
            float halfHeight = view.orthographic
                ? view.orthographicSize
                : distance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);

            Vector3 world = ViewSpaceMapping.UnitToGround(unitPoint, center, euler.y, tilt, halfHeight, view.aspect,
                MaxTilt, distance);
            return ClampToFocus(focus, world);
        }

        // Pulls the point into the MinDistance..MaxDistance ring around the focus, keeping its direction.
        private Vector3 ClampToFocus(Vector3 focus, Vector3 world)
        {
            Vector3 offset = world - focus;
            offset.y = 0f;
            float distance = offset.magnitude;
            float clamped = Mathf.Clamp(distance, MinDistance, Mathf.Max(MinDistance, MaxDistance));
            if (Mathf.Approximately(distance, clamped)) return world;

            Vector3 direction = distance > 1e-3f ? offset / distance : Vector3.forward;
            Vector3 result = focus + direction * clamped;
            result.y = world.y;
            return result;
        }

        private Vector3 Sample(Vector3 focus, in SpawnHint hint)
        {
            UnityEngine.Camera view = CameraManager.GetCamera();
            if (view == null) return SampleWithoutCamera(focus, hint);

            // 1) Angle on the unit circle: around the heading as seen on screen, or anywhere.
            float angle;
            if (hint.HasDirection && TryGetScreenAngle(view, focus, hint.Direction, out float heading))
            {
                float halfArc = Mathf.Lerp(180f, SpreadAngle, Mathf.Clamp01(hint.Weight));
                angle = heading + UnityEngine.Random.Range(-halfArc, halfArc);
            }
            else
            {
                angle = UnityEngine.Random.Range(0f, 360f);
            }

            // 2) Radius on the unit circle.
            float max = Mathf.Max(MinScale, MaxScale);
            float radius = UniformByArea
                ? Mathf.Sqrt(UnityEngine.Random.Range(MinScale * MinScale, max * max))
                : UnityEngine.Random.Range(MinScale, max);

            // 3) Unit circle -> ground.
            return MapToGround(view, ViewSpaceMapping.FromPolar(angle, radius), focus);
        }

        /// <summary>
        /// The angle (0 = up on screen, 90 = right) the world direction has on the view unit circle, taken from
        /// where the focus lands on screen when it moves along that direction.
        /// </summary>
        private static bool TryGetScreenAngle(UnityEngine.Camera view, Vector3 focus, Vector3 worldDirection, out float degrees)
        {
            degrees = 0f;
            Vector3 from = view.WorldToViewportPoint(focus);
            Vector3 to = view.WorldToViewportPoint(focus + worldDirection.normalized * ProbeDistance);
            if (from.z <= 0f || to.z <= 0f) return false;

            // Viewport 0..1 maps to the unit square -1..1 on both axes, so the angle is the same in either.
            Vector2 delta = new Vector2(to.x - from.x, to.y - from.y);
            if (delta.sqrMagnitude < 1e-10f) return false;

            degrees = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            return true;
        }

        private Vector3 SampleWithoutCamera(Vector3 focus, in SpawnHint hint)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            if (hint.HasDirection)
            {
                float halfArc = Mathf.Lerp(180f, SpreadAngle, Mathf.Clamp01(hint.Weight));
                angle = UnityEngine.Random.Range(-halfArc, halfArc);
            }

            Vector3 baseDirection = hint.HasDirection ? hint.Direction.normalized : Vector3.forward;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            return focus + direction * UnityEngine.Random.Range(MinDistance, Mathf.Max(MinDistance, MaxDistance));
        }
    }
}
