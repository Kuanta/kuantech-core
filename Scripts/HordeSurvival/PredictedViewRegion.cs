using System;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// The part of the ground that is on screen now or will be within a short horizon, given where the focus
    /// (the thing the camera follows) is heading. A point belongs to it when, at some moment in the horizon,
    /// the screen would be showing it.
    ///
    /// The test is forward projection only: the camera follows the focus, so at time t the screen footprint has
    /// moved by the focus' predicted displacement d(t), and "p is visible at t" is "p - d(t) is inside the
    /// viewport right now". Nothing is inverted and nothing is ray cast. Moving straight, the sideways parts the
    /// screen will never reach drop out by themselves; turning, the region follows the curve.
    /// </summary>
    [Serializable]
    public class PredictedViewRegion
    {
        [Tooltip("How far ahead, in seconds, the focus' path is followed to see what will come on screen.")]
        public float HorizonSeconds = 2f;
        [Tooltip("How many moments along the horizon are tested (plus now). More = smoother region, more cost.")]
        [Range(1, 12)]
        public int TimeSamples = 5;
        [Tooltip("Grows (+) or shrinks (-) the screen rectangle, in viewport units. 0 = exactly the screen, " +
                 "0.1 = a tenth of the screen beyond each edge.")]
        public float ViewMargin = 0f;

        /// <summary>
        /// Displacement of the focus after <paramref name="time"/> seconds if it keeps its speed and keeps
        /// turning at <paramref name="turnRateDegrees"/> per second (a constant-turn arc, a straight line when
        /// the turn is negligible). Heading convention is Unity's yaw: forward = (sin, cos) on (x, z).
        /// </summary>
        public static Vector3 PredictDisplacement(Vector3 velocity, float turnRateDegrees, float time)
        {
            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed < 0.01f) return Vector3.zero;

            float omega = turnRateDegrees * Mathf.Deg2Rad;
            if (Mathf.Abs(omega) < 0.01f) return velocity * time;

            float heading = Mathf.Atan2(velocity.x, velocity.z);
            float turned = heading + omega * time;
            float radius = speed / omega;
            return new Vector3(
                radius * (Mathf.Cos(heading) - Mathf.Cos(turned)),
                0f,
                radius * (Mathf.Sin(turned) - Mathf.Sin(heading)));
        }

        // Everything below is per-frame state. Contains is called hundreds of times for a single spawn point
        // (candidates x time samples) and a relocation pass asks for many points in one frame, but the camera
        // and the focus' motion do not change within a frame. Computing them once, and doing the projection
        // in managed code instead of one native WorldToViewportPoint per sample, is what keeps that cheap.
        [NonSerialized] private int _cacheFrame = -1;
        [NonSerialized] private UnityEngine.Camera _cacheCamera;
        [NonSerialized] private Matrix4x4 _viewProjection;
        [NonSerialized] private Vector3 _cacheVelocity;
        [NonSerialized] private float _cacheTurnRate;
        [NonSerialized] private int _cacheSamples;
        [NonSerialized] private Vector3[] _displacements;

        private void EnsureFrameCache(UnityEngine.Camera view, Vector3 velocity, float turnRateDegrees)
        {
            int frame = Time.frameCount;
            int samples = Mathf.Max(1, TimeSamples);
            bool sameFrame = _cacheFrame == frame && _cacheCamera == view;
            bool sameMotion = sameFrame && _cacheSamples == samples
                              && _cacheVelocity == velocity && _cacheTurnRate == turnRateDegrees;
            if (sameMotion) return;

            if (!sameFrame)
            {
                _cacheFrame = frame;
                _cacheCamera = view;
                _viewProjection = view.projectionMatrix * view.worldToCameraMatrix;
            }

            if (_displacements == null || _displacements.Length != samples + 1)
                _displacements = new Vector3[samples + 1];
            for (int i = 0; i <= samples; i++)
                _displacements[i] = PredictDisplacement(velocity, turnRateDegrees, HorizonSeconds * i / samples);
            _cacheSamples = samples;
            _cacheVelocity = velocity;
            _cacheTurnRate = turnRateDegrees;
        }

        /// <summary>
        /// True when <paramref name="point"/> is on screen at some moment of the horizon. firstTime is the first
        /// such moment: 0 = on screen right now, larger = it will come into view later.
        /// </summary>
        public bool Contains(UnityEngine.Camera view, Vector3 point, Vector3 velocity, float turnRateDegrees, out float firstTime)
        {
            EnsureFrameCache(view, velocity, turnRateDegrees);

            int samples = _cacheSamples;
            for (int i = 0; i <= samples; i++)
            {
                Vector3 shifted = point - _displacements[i];
                if (IsOnScreen(view, shifted, ViewMargin))
                {
                    firstTime = HorizonSeconds * i / samples;
                    return true;
                }
            }
            firstTime = float.MaxValue;
            return false;
        }

        /// <summary>Whether <paramref name="world"/> is inside the viewport right now (no margin, no forecast).</summary>
        public bool IsOnScreenNow(UnityEngine.Camera view, Vector3 world)
        {
            if (!view.orthographic && (_cacheFrame != Time.frameCount || _cacheCamera != view))
                EnsureFrameCache(view, _cacheVelocity, _cacheTurnRate);
            return IsOnScreen(view, world, 0f);
        }

        private bool IsOnScreen(UnityEngine.Camera view, Vector3 world, float margin)
        {
            if (view.orthographic)
            {
                Vector3 viewport = view.WorldToViewportPoint(world);
                if (viewport.z <= 0f) return false;
                return viewport.x >= -margin && viewport.x <= 1f + margin
                    && viewport.y >= -margin && viewport.y <= 1f + margin;
            }

            Matrix4x4 m = _viewProjection;
            float w = m.m30 * world.x + m.m31 * world.y + m.m32 * world.z + m.m33;
            if (w <= 0f) return false; // behind the camera
            float x = (m.m00 * world.x + m.m01 * world.y + m.m02 * world.z + m.m03) / w * 0.5f + 0.5f;
            float y = (m.m10 * world.x + m.m11 * world.y + m.m12 * world.z + m.m13) / w * 0.5f + 0.5f;
            return x >= -margin && x <= 1f + margin
                && y >= -margin && y <= 1f + margin;
        }
    }
}
