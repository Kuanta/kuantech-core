using System.Collections.Generic;
using System.Numerics;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Kuantech.Core.Combat
{
    public static class CombatUtilities
    {

        #region Projectiles

        public static void ShootProjectile(Projectile projectile, Vector3 shootPosition, Vector3 shootDirection, Transform target, Actor shooter)
        {
            projectile.Shoot(shooter, null, shootPosition, shootDirection, target);
        }

        #endregion

        #region Cast Overlap attacks

        // Every query below only knows two things: the physics layer to search, and whether the candidate
        // itself says it's willing to be hit right now (IHittable.CanBeHit()). It has no concept of alive/
        // dead or faction — a dead Actor's CanBeHit() is still true (see Actor.cs), so a corpse is exactly as
        // findable as a live enemy. Whether a hit actually DOES anything (damage, faction relevance, an
        // already-dead actor's health not moving) is decided downstream: Actor.OnHit for faction, and
        // HealthcareModule for whether the actor is alive enough to take damage.

        /// <summary>
        /// Gets hittables in 2d circle
        /// </summary>
        public static List<IHittable> GetHittablesInCircle2D(Vector3 position, float radius, LayerMask layerMask)
        {
            Collider2D[] hits = UnityEngine.Physics2D.OverlapCircleAll(position, radius, layerMask);
            List<IHittable> hittables = new();

            foreach (var hit in hits)
            {
                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                hittables.Add(hittable);
            }

            return hittables;
        }

        public static List<IHittable> GetHittablesInSphere(Vector3 position, float radius, LayerMask layerMask)
        {
            Collider[] hits = UnityEngine.Physics.OverlapSphere(position, radius, layerMask);
            List<IHittable> hittables = new List<IHittable>();
            foreach (var hit in hits)
            {
                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                hittables.Add(hittable);
            }

            return hittables;
        }

        /// <summary>
        /// Gets hittables inside a capsule between two points — used for melee weapon sweeps (WeaponVisual
        /// queries this every active-window frame between its StartSweep/EndSweep points). The radius is
        /// meant to be generous, not mesh-accurate: a swing's "did it connect" feel comes from the timing
        /// matching the animation, not from pixel-precise geometry.
        /// </summary>
        public static List<IHittable> GetHittablesInCapsule(Vector3 start, Vector3 end, float radius, LayerMask layerMask)
        {
            Collider[] hits = UnityEngine.Physics.OverlapCapsule(start, end, radius, layerMask);
            List<IHittable> hittables = new List<IHittable>();
            foreach (var hit in hits)
            {
                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                hittables.Add(hittable);
            }

            return hittables;
        }

        /// <summary>
        /// Returns hittables in a linear box
        /// </summary>
        public static List<IHittable> GetHittablesInBox(Vector3 startPosition, Vector3 direction, float width, float range, LayerMask layerMask,
            float boxHeight = 2f)
        {
            List<IHittable> hittables = new List<IHittable>();
            Vector3 center = startPosition + direction.normalized * range * 0.5f;
            Vector3 halfSizes = new Vector3(width * 0.5f, boxHeight * 0.5f, range * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Collider[] hits = UnityEngine.Physics.OverlapBox(center, halfSizes, rotation, layerMask);
            foreach (var hit in hits)
            {
                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                hittables.Add(hittable);
            }

            return hittables;
        }

        /// <summary>
        /// Gets hittables in a 2d arc
        /// </summary>
        /// <param name="backOffset">Back offset to consider actors that are very close to center</param>
        public static List<IHittable> GetHittablesInArc2D(
            Vector2 center,
            Vector2 direction,
            float range,
            float angle,
            LayerMask layerMask,
            float backOffset = 0.5f,
            float forwardGuard = 0f,
            bool useClosestPoint = true)
        {
            var detected = new List<IHittable>();

            // Yönü normalize et, boşsa default ver
            var dir = direction.sqrMagnitude < 1e-6f ? Vector2.right : direction.normalized;

            // Açı eşiği (yarım açı kosinüsü)
            float cosHalf = Mathf.Cos(0.5f * angle * Mathf.Deg2Rad);
            float rangeSqr = range * range;

            // Açı testinin yapılacağı "apex"i biraz geriye taşı
            Vector2 apex = center - dir * backOffset;

            // Adayları kaçırmamak için arama yarıçapını biraz genişlet
            var results = Physics2D.OverlapCircleAll(apex, range + backOffset, layerMask);

            foreach (var col in results)
            {
                if (!col) continue;
                IHittable hittable = col.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;

                // Test noktası: collider'ın merkezi yerine en yakın nokta daha güvenilir
                Vector2 p = useClosestPoint ? col.ClosestPoint(center) : (Vector2)col.transform.position;

                // "Önde mi?" koruması (orijinal merkez referansıyla)
                float proj = Vector2.Dot(p - center, dir);
                if (proj < forwardGuard) continue;

                // Açı/distance testi apex'e göre
                Vector2 v = p - apex;
                if (v.sqrMagnitude > rangeSqr) continue;

                float dot = Vector2.Dot(dir, v.normalized); // cos(theta)
                if (dot >= cosHalf)
                    detected.Add(hittable);
            }

            return detected;
        }

        public static List<IHittable> GetHittablesInArc3D(
            Vector3 center,
            Vector3 direction,
            float range,
            float angle,
            LayerMask layerMask,
            float backOffset = 0.5f,
            float forwardGuard = 0f,
            bool useClosestPoint = true,
            int maxActorCount = 128)
        {
            var detected = new List<IHittable>();

            var dir = direction.sqrMagnitude < 1e-6f ? Vector3.right : direction.normalized;

            float cosHalf = Mathf.Cos(0.5f * angle * Mathf.Deg2Rad);
            float rangeSqr = range * range;

            // Move aapex
            Vector3 apex = center - dir * backOffset;

            // Adayları kaçırmamak için arama yarıçapını biraz genişlet
            Collider[] results = new Collider[maxActorCount];

            if (UnityEngine.Physics.OverlapSphereNonAlloc(apex, range + backOffset, results, layerMask) > 0)
            {
                foreach (var col in results)
                {
                    if (!col) continue;
                    IHittable hittable = col.GetComponentInParent<IHittable>();
                    if (hittable == null || !hittable.CanBeHit()) continue;

                    // Test point
                    Vector3 p = useClosestPoint ? col.ClosestPoint(center) : col.transform.position;

                    float proj = Vector3.Dot(p - center, dir);
                    if (proj < forwardGuard) continue;

                    // Açı/distance testi apex'e göre
                    Vector3 v = p - apex;
                    if (v.sqrMagnitude > rangeSqr) continue;

                    float dot = Vector3.Dot(dir, v.normalized); // cos(theta)
                    if (dot >= cosHalf)
                        detected.Add(hittable);
                }
            }
            return detected;
        }

        // Excludes hitInfo.Hitter from the results — the one thing left for this layer to enforce itself
        // (a query cannot know about factions any more, but "don't hit your own caster" needs no faction
        // lookup, just an identity check against who's swinging).
        private static bool IsSelfHit(IHittable hittable, GameObject hitter)
        {
            if (hitter == null) return false;
            if (hittable is Component component) return component.gameObject == hitter;
            return false;
        }

        public static void HitInSphere(Vector3 center, float radius, LayerMask layerMask, HitInfo hitInfo,
            UnityAction<IHittable> damageHandler = null)
        {
            Collider[] results = UnityEngine.Physics.OverlapSphere(center, radius, layerMask.value);
            foreach (var result in results)
            {
                if (result == null) continue;
                IHittable hittable = result.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                hittable.OnHit(hitInfo);
                damageHandler?.Invoke(hittable);
            }
        }

        public static void HitInCircle2D(Vector3 center, float range,
            LayerMask layerMask, HitInfo hitInfo, UnityAction<IHittable> damageHandler = null)
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(center, range, layerMask.value);
            foreach (var result in results)
            {
                if (result == null) continue;
                IHittable hittable = result.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                hittable.OnHit(hitInfo);
                damageHandler?.Invoke(hittable);
            }
        }

        public static void HitInArc2D(Vector3 center, Vector3 direction, float range, float angle,
            LayerMask layerMask, HitInfo hitInfo, UnityAction<IHittable> damageHandler = null)
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(center, range, layerMask.value);
            foreach (var result in results)
            {
                if (result == null) continue;
                IHittable hittable = result.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                //Check angle
                Vector3 toTarget = result.transform.position - center;
                float angleTo = Vector2.Angle(direction, toTarget);
                if (angleTo <= angle * 0.5f)
                {
                    hittable.OnHit(hitInfo);
                    damageHandler?.Invoke(hittable);
                }
            }
        }

        public static void HitInArc3D(Vector3 center,
            Vector3 direction,
            float range,
            float angle,
            LayerMask layerMask,
            HitInfo hitInfo,
            UnityAction<IHittable> damageHandler = null,
            float backOffset = 0.5f,
            float forwardGuard = 0f,
            bool useClosestPoint = true,
            int maxActorCount = 128)
        {
            var hittables = GetHittablesInArc3D(center, direction, range, angle, layerMask, backOffset, forwardGuard, useClosestPoint, maxActorCount);
            foreach (var hittable in hittables)
            {
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                hittable.OnHit(hitInfo);
                damageHandler?.Invoke(hittable);
            }
        }

        /// <summary>
        /// Hits hittables in a box
        /// </summary>
        public static void HitInBox2D(Vector3 startPosition, Vector3 direction, float width, float length, LayerMask layerMask,
            HitInfo hitInfo, UnityAction<IHittable> damageHandler = null)
        {
            List<IHittable> hittables = GetHittablesInBox2D(startPosition, direction, width, length, layerMask);
            foreach (var hittable in hittables)
            {
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                hittable.OnHit(hitInfo);
                damageHandler?.Invoke(hittable);
            }
        }

        /// <summary>
        /// Gets hittables in a 2d box
        /// </summary>
        public static List<IHittable> GetHittablesInBox2D(Vector3 startPosition, Vector3 direction, float width, float length,
            LayerMask layerMask)
        {
            List<IHittable> hittables = new List<IHittable>();
            direction.z = 0;
            direction.Normalize();
            Vector3 boxCenter = startPosition + direction * length * 0.5f;
            // Get angle for the box rotation (only Z needed)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Size of the box (length along direction, width perpendicular)
            Vector2 boxSize = new Vector2(length, width);

            // Perform the box overlap
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, layerMask);
            if (hits.IsNullOrEmpty()) return hittables;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;

                hittables.Add(hittable);
            }

            return hittables;
        }

        public static List<IHittable> GetHittablesInRaycast2D(Vector3 startPosition, Vector3 direction, float range,
            LayerMask layerMask)
        {
            List<IHittable> hittables = new List<IHittable>();
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction, range, layerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                IHittable hittable = hit.collider.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                hittables.Add(hittable);
            }

            return hittables;
        }
        public static void HitInRaycast2D(Vector3 startPosition, Vector3 direction, float range,
            LayerMask layerMask, HitInfo hitInfo, UnityAction<IHittable> damageHandler = null)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction, range, layerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                IHittable hittable = hit.collider.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit()) continue;
                if (IsSelfHit(hittable, hitInfo.Hitter)) continue;

                hittable.OnHit(hitInfo);
                damageHandler?.Invoke(hittable);
            }
        }
        #endregion

        #region Attack Timing

        public static float GetAttackDuration(float attackSpeed, float baseAttackTime, float minAttackTime,
            float maxAttackTime)
        {
            float attackRate = attackSpeed / (100 * baseAttackTime);
            float attackDuration = Mathf.Clamp(1 / attackRate, minAttackTime, maxAttackTime);
            return attackDuration;
        }

        /// <summary>
        /// Returns the time multiplier for attack speed. The more attack speed the less this multiplier becomes.
        /// More attack speed, reduces the time taken for every part of an attack
        /// </summary>
        /// <returns></returns>
        public static float GetAttackSpeedMultiplier(float attackSpeed, float baseAttackTime, float minAttackTime,
            float maxAttackTime)
        {
            float reducedAttackTime = GetAttackDuration(attackSpeed, baseAttackTime, minAttackTime, maxAttackTime);
            return attackSpeed / reducedAttackTime;
        }
        #endregion
    }
}
