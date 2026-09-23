using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Says which body part a collider represents. Sits beside the collider on a bone; the hit queries find
    /// it from whatever collider they caught.
    ///
    /// Deliberately holds NO owner reference. Every hit query already walks up with
    /// GetComponentInParent&lt;IHittable&gt;() to find the actor, and a cached owner would be actively wrong
    /// here: these colliders live on the ActorVisual, which is pooled and re-parented to a different actor
    /// over its life. The walk is the correct answer, not an optimisation to avoid.
    ///
    /// So this component is pure zone data -- which is also why it is safe to leave on a collider belonging
    /// to an actor that has no parts authored at all.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour
    {
        public BodyPartAsset BodyPart;

        /// <summary>
        /// The part of whatever collider was actually struck, or null when it has none. Static because
        /// callers hold a Collider rather than a HitBox, and most colliders in the world carry neither.
        /// </summary>
        public static BodyPartAsset ResolveBodyPart(Component hitCollider)
        {
            if (hitCollider == null) return null;
            HitBox hitBox = hitCollider.GetComponent<HitBox>();
            return hitBox != null ? hitBox.BodyPart : null;
        }

        /// <summary>1 for no part, so an unconfigured rig behaves exactly as it does today.</summary>
        public static float GetDamageMultiplier(BodyPartAsset bodyPart)
        {
            return bodyPart != null ? bodyPart.DamageMultiplier : 1f;
        }

        // Authoring aid: hit volumes are tuned by eye -- a head hitbox is deliberately bigger than the head
        // it covers -- and that is impossible to judge without seeing them against the mesh.
        private void OnDrawGizmosSelected()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = BodyPart != null ? BodyPart.MainColor : Color.white;
            Bounds bounds = col.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
