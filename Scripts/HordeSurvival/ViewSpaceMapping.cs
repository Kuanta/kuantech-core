using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Maps a point of the "view unit square" onto the ground, from nothing but the camera's pose and lens.
    /// No rays: it is a pipeline of three small transforms, T3(T2(T1(P))), then yaw and a move to the ground
    /// point under the screen center.
    ///
    /// The unit circle is the ellipse inscribed in the viewport: (0,0) is the screen center, (+-1,0) and
    /// (0,+-1) touch the left/right and top/bottom screen edges. Scale it (p * 1.2 = a band outside the
    /// screen) and run it through <see cref="UnitToGround"/>.
    ///
    ///   T1  screen size + aspect ratio      scale both axes (square -> screen rectangle, in world units)
    ///   T2  pitch                           scale the vertical axis by 1 / cos(tilt)
    ///   T3  perspective (field of view)     divide both axes by w = 1 - y * sin(tilt) / D, which pushes the
    ///                                       far half of the screen away and pulls the near half in (an egg)
    ///
    /// Without T3 (orthographic camera) the ring is a symmetric ellipse. D is the distance from the camera to
    /// the ground point under the screen center; as D grows w goes to 1 and perspective turns into ortho.
    /// The function takes plain numbers (no Camera), so it is trivial to test and to reuse.
    /// </summary>
    public static class ViewSpaceMapping
    {
        // Floor for the perspective divisor, so points at or above the horizon land very far instead of
        // dividing by zero or flipping sides (0.05 = at most 20x stretch).
        private const float MinPerspectiveW = 0.05f;

        /// <summary>
        /// A point on the unit circle from a screen angle (0 = up on screen, 90 = right) and a radius
        /// (1 = touches the screen edges).
        /// </summary>
        public static Vector2 FromPolar(float degrees, float radius)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
        }

        /// <summary>
        /// T1: view unit square to the screen plane, in world units. The screen is 2*halfHeight tall and
        /// 2*halfHeight*aspect wide, so this is where size and aspect ratio come in.
        /// </summary>
        public static Vector2 ApplyScreenSize(Vector2 p, float halfHeight, float aspect)
        {
            return new Vector2(p.x * halfHeight * aspect, p.y * halfHeight);
        }

        /// <summary>
        /// T2: pitch. A camera tilted by alpha from straight down (alpha = 0: top-down) looks at the ground
        /// at a slant, so the screen's vertical axis is stretched by 1 / cos(alpha) on the ground. The
        /// horizontal axis is untouched. With parallel rays this is symmetric around the center.
        /// </summary>
        public static Vector2 ApplyPitch(Vector2 p, float tiltDegrees)
        {
            float cos = Mathf.Max(Mathf.Cos(tiltDegrees * Mathf.Deg2Rad), 0.05f);
            return new Vector2(p.x, p.y / cos);
        }

        /// <summary>
        /// T3: perspective. <paramref name="p"/> is the output of T2, <paramref name="distance"/> is D, the
        /// distance from the camera to the ground point under the screen center. Divides both axes by
        /// w = 1 - p.y * sin(tilt) / D: above the center (p.y > 0) w < 1 and the point moves away, below it
        /// w > 1 and it comes closer. A distance of 0 or less means orthographic: nothing happens.
        /// </summary>
        public static Vector2 ApplyPerspective(Vector2 p, float tiltDegrees, float distance)
        {
            if (distance <= 0f) return p;
            float w = 1f - p.y * Mathf.Sin(tiltDegrees * Mathf.Deg2Rad) / distance;
            return p / Mathf.Max(w, MinPerspectiveW);
        }

        /// <summary>
        /// The ground point for view-space position <paramref name="p"/>: T3(T2(T1(p))), turned by yaw and
        /// moved to <paramref name="center"/>.
        /// </summary>
        /// <param name="center">Ground point under the center of the screen.</param>
        /// <param name="yawDegrees">Camera yaw (Unity euler Y).</param>
        /// <param name="tiltDegrees">Camera tilt from straight down: 0 = looking down, 90 = at the horizon.</param>
        /// <param name="halfHeight">Half the screen height in world units at the center: the orthographic size, or
        /// D * tan(fov / 2) for a perspective camera.</param>
        /// <param name="aspect">Viewport width / height.</param>
        /// <param name="maxTiltDegrees">Tilt above this is clamped, so the ground never gets stretched to infinity.</param>
        /// <param name="perspectiveDistance">D for a perspective camera, 0 for an orthographic one.</param>
        public static Vector3 UnitToGround(Vector2 p, Vector3 center, float yawDegrees, float tiltDegrees,
            float halfHeight, float aspect, float maxTiltDegrees, float perspectiveDistance)
        {
            float tilt = Mathf.Clamp(tiltDegrees, 0f, maxTiltDegrees);
            Vector2 local = ApplyPerspective(ApplyPitch(ApplyScreenSize(p, halfHeight, aspect), tilt), tilt, perspectiveDistance);

            float yaw = yawDegrees * Mathf.Deg2Rad;
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            Vector3 forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            return center + right * local.x + forward * local.y;
        }

        /// <summary>
        /// The ground point under the center of the screen: where the camera's forward line meets the ground
        /// (plain line/plane math). Falls back to the point under the camera when it looks level or upward.
        /// </summary>
        public static Vector3 ScreenCenterOnGround(Vector3 cameraPosition, Vector3 cameraForward, float groundY)
        {
            if (cameraForward.y < -0.0001f)
            {
                float t = (groundY - cameraPosition.y) / cameraForward.y;
                if (t > 0f)
                {
                    Vector3 hit = cameraPosition + cameraForward * t;
                    hit.y = groundY;
                    return hit;
                }
            }
            return new Vector3(cameraPosition.x, groundY, cameraPosition.z);
        }
    }
}
