using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// A tiny physics-free ballistic simulation: a body flying under gravity that bounces off a flat
    /// ground plane and settles once it runs out of energy. Drive it with <see cref="Step"/> each frame
    /// and read <see cref="Position"/>. Lets loot orbs, yeeted bodies, debris, etc. arc + bounce without a
    /// Rigidbody or colliders — cheap enough for hundreds of instances on mobile.
    ///
    /// Held as a field and mutated in place: call the methods on the field directly.
    /// </summary>
    [Serializable]
    public struct BallisticMotion
    {
        [Tooltip("Downward acceleration. Higher = snappier arc.")]
        public float Gravity;
        [Tooltip("Vertical speed kept after a bounce (0 = no bounce, 1 = perfectly bouncy).")]
        public float Bounciness;
        [Tooltip("Horizontal speed kept after a bounce.")]
        public float GroundFriction;
        [Tooltip("Once the body is slower than this, it settles and stops.")]
        public float SettleSpeed;

        //State
        public Vector3 Position;
        public Vector3 HorizontalVelocity;
        public float VerticalVelocity;
        public float GroundY;
        public bool Settled;

        public BallisticMotion(float gravity, float bounciness, float groundFriction, float settleSpeed)
        {
            Gravity = gravity;
            Bounciness = bounciness;
            GroundFriction = groundFriction;
            SettleSpeed = settleSpeed;

            Position = Vector3.zero;
            HorizontalVelocity = Vector3.zero;
            VerticalVelocity = 0f;
            GroundY = 0f;
            Settled = false;
        }

        /// <summary>Starts a new flight. Tuning fields (Gravity, etc.) are expected to be set already.</summary>
        public void Launch(Vector3 startPos, Vector3 horizontalVelocity, float verticalVelocity, float groundY)
        {
            Position = startPos;
            HorizontalVelocity = horizontalVelocity;
            VerticalVelocity = verticalVelocity;
            GroundY = groundY;
            Settled = false;
        }

        /// <summary>
        /// Advances one step. Returns true on the frame a ground bounce happens (handy for squash/FX).
        /// </summary>
        public bool Step(float dt)
        {
            if (Settled) return false;

            VerticalVelocity -= Gravity * dt;
            Position += HorizontalVelocity * dt;
            Position.y += VerticalVelocity * dt;

            bool bounced = false;
            if (Position.y <= GroundY)
            {
                Position.y = GroundY;
                if (VerticalVelocity < 0f)
                {
                    VerticalVelocity = -VerticalVelocity * Bounciness;
                    HorizontalVelocity *= GroundFriction;
                    bounced = true;
                }

                if (VerticalVelocity < SettleSpeed && HorizontalVelocity.sqrMagnitude < SettleSpeed * SettleSpeed)
                {
                    Settled = true;
                    VerticalVelocity = 0f;
                    HorizontalVelocity = Vector3.zero;
                }
            }
            return bounced;
        }
    }
}
