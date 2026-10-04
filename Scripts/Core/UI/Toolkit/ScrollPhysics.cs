using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// The feel of a list that is dragged with a finger, as plain numbers (no UI in it, so it can be tested): the content follows the
    /// pointer one to one; pulled past an end it follows with a growing resistance (a rubber band); let go with some speed it keeps
    /// going and slows down; if it runs into an end it bounces a little; and anything pulled past an end springs back.
    ///
    /// The owner feeds it: <see cref="Drag"/> while the pointer holds the content, <see cref="Release"/> when it lets go, and
    /// <see cref="Step"/> every frame after that until it says it is still. What to show comes out as <see cref="Offset"/> (how far
    /// the list is scrolled, always inside 0 to <see cref="Max"/>) and <see cref="Overscroll"/> (how far the content is moved
    /// on top of that, past an end).
    /// </summary>
    public class ScrollPhysics
    {
        /// <summary>The farthest the content can be pulled past an end (it gets harder to pull the nearer it gets).</summary>
        public float OverscrollLimit = 160f;

        /// <summary>The share of its speed a moving list has left after one second (0.135 is what UI Toolkit's own touch scrolling uses).</summary>
        public float DecelerationRate = 0.135f;

        /// <summary>How fast a list pulled past an end goes back (higher is snappier).</summary>
        public float SpringRate = 14f;

        /// <summary>A list slower than this (units per second) stops.</summary>
        public float StopSpeed = 15f;

        /// <summary>How far a list that runs into an end with some speed goes past it, as a share of that speed.</summary>
        public float BounceFactor = 0.1f;

        /// <summary>The farthest the list can be scrolled (the content's height minus the view's). Keep it up to date.</summary>
        public float Max;

        /// <summary>How far the list is scrolled, from 0 to <see cref="Max"/>.</summary>
        public float Offset { get; private set; }

        /// <summary>How far the content is shown past an end: above 0 it is pulled down past the top, below 0 it is pulled up past the bottom.</summary>
        public float Overscroll { get; private set; }

        /// <summary>How fast the offset is changing, in units per second (positive: scrolling down).</summary>
        public float Velocity { get; private set; }

        /// <summary>Whether <see cref="Step"/> still has something to move.</summary>
        public bool IsMoving => Velocity != 0f || Overscroll != 0f;

        /// <summary>How far the content is shown for a pull of this size past an end: about half of it at first, and never more than the limit.</summary>
        public float Rubber(float excess)
        {
            return OverscrollLimit * (1f - Mathf.Exp(-excess / (2f * OverscrollLimit)));
        }

        /// <summary>Puts the list at an offset at once (the wheel, code). Stops everything.</summary>
        public void SetOffset(float offset)
        {
            Offset = Mathf.Clamp(offset, 0f, Mathf.Max(0f, Max));
            Overscroll = 0f;
            Velocity = 0f;
        }

        /// <summary>The pointer holds the content: where the list would be if there were no ends (how far it was dragged from where it started).</summary>
        public void Drag(float rawOffset)
        {
            Velocity = 0f;
            float max = Mathf.Max(0f, Max);

            if (rawOffset < 0f)
            {
                Offset = 0f;
                Overscroll = Rubber(-rawOffset);
            }
            else if (rawOffset > max)
            {
                Offset = max;
                Overscroll = -Rubber(rawOffset - max);
            }
            else
            {
                Offset = rawOffset;
                Overscroll = 0f;
            }
        }

        /// <summary>The pointer lets go with this speed (units per second, positive: scrolling down).</summary>
        public void Release(float velocity)
        {
            // With nothing to scroll there is nothing to carry on with.
            Velocity = Max > 0f ? velocity : 0f;
        }

        /// <summary>Moves on by dt seconds. Returns whether it is still moving.</summary>
        public bool Step(float dt)
        {
            if (Overscroll != 0f)
            {
                // Past an end, the content only goes back.
                Overscroll *= Mathf.Exp(-SpringRate * dt);
                if (Mathf.Abs(Overscroll) < 0.5f) Overscroll = 0f;
                return Overscroll != 0f;
            }

            if (Mathf.Abs(Velocity) < StopSpeed)
            {
                Velocity = 0f;
                return false;
            }

            Offset += Velocity * dt;
            Velocity *= Mathf.Pow(DecelerationRate, dt);

            float max = Mathf.Max(0f, Max);
            if (Offset < 0f || Offset > max)
            {
                // It ran into an end: it goes a little past it (in proportion to its speed) and springs back.
                bool top = Offset < 0f;
                float bounce = Mathf.Min(Mathf.Abs(Velocity) * BounceFactor, OverscrollLimit * 0.6f);
                Offset = top ? 0f : max;
                Overscroll = top ? bounce : -bounce;
                Velocity = 0f;
            }
            return true;
        }
    }
}
