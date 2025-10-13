using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

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
 /// <summary>
        /// Gets actors in 2d circle
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="layerMask"></param>
        /// <param name="factionFilter">Faction ids to filter Out</param>
        /// <returns></returns>
        public static List<Actor> GetActorsInCircle2D(Vector3 position, float radius, LayerMask layerMask, HashSet<int> factionFilter = null)
        {
            Collider2D[] hits = UnityEngine.Physics2D.OverlapCircleAll(position, radius, layerMask);
            List<Actor> actors = new();

            foreach (var hit in hits)
            {
                Actor actor = hit.GetComponentInParent<Actor>();
                if (actor == null) continue;
                if(factionFilter != null && !factionFilter.Contains(actor.GetFactionId())) continue;
                actors.Add(actor);
            }

            return actors;
        }

         public static List<Actor> GetActorsInSphere(Vector3 position, float radius, LayerMask layerMask,
             HashSet<int> factionFilter = null)
         {
             Collider[] hits = UnityEngine.Physics.OverlapSphere(position, radius, layerMask);
             List<Actor> actors = new List<Actor>();
             foreach (var hit in hits)
             {
                 Actor actor = hit.GetComponentInParent<Actor>();
                 if (actor == null) continue;
                 if(factionFilter != null && !factionFilter.Contains(actor.GetFactionId())) continue;
                 actors.Add(actor);
             }

             return actors;
             
         }
        
        /// <summary>
        /// Gets actors in a 2d arc
        /// </summary>
        /// <param name="center"></param>
        /// <param name="direction"></param>
        /// <param name="range"></param>
        /// <param name="angle"></param>
        /// <param name="layerMask"></param>
        /// <param name="backOffset">Back offset to consider actors that are very close to center</param>
        /// <param name="factionFilter"></param>
        /// <returns></returns>
        public static List<Actor> GetActorsInArc2D(
            Vector2 center,
            Vector2 direction,
            float range,
            float angle,
            LayerMask layerMask,
            HashSet<int> factionFilter = null,
            float backOffset = 0.5f, 
            float forwardGuard = 0f,
            bool useClosestPoint = true)
        {
            var detected = new List<Actor>();

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
                if (!col || !col.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive()) continue;
                int f = actor.GetFactionId();
                if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(f)) continue;

                // Test noktası: collider'ın merkezi yerine en yakın nokta daha güvenilir
                Vector2 p = useClosestPoint ? col.ClosestPoint(center) : (Vector2)actor.GetActorLocation();

                // "Önde mi?" koruması (orijinal merkez referansıyla)
                float proj = Vector2.Dot(p - center, dir);
                if (proj < forwardGuard) continue;

                // Açı/distance testi apex'e göre
                Vector2 v = p - apex;
                if (v.sqrMagnitude > rangeSqr) continue;

                float dot = Vector2.Dot(dir, v.normalized); // cos(theta)
                if (dot >= cosHalf)
                    detected.Add(actor);
            }

            return detected;
        }

        public static List<Actor> GetActorsInArc3D(
            Vector3 center,
            Vector3 direction,
            float range,
            float angle,
            LayerMask layerMask,
            HashSet<int> factionFilter = null,
            float backOffset = 0.5f,
            float forwardGuard = 0f,
            bool useClosestPoint = true,
            int maxActorCount = 128)
        {
            var detected = new List<Actor>();

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
                    if (!col || !col.TryGetComponent(out Actor actor)) continue;
                    if (!actor.IsAlive()) continue;
                    int f = actor.GetFactionId();
                    if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(f)) continue;

                    // Test point
                    Vector3 p = useClosestPoint ? col.ClosestPoint(center) : (Vector3)actor.GetActorLocation();

                    float proj = Vector3.Dot(p - center, dir);
                    if (proj < forwardGuard) continue;

                    // Açı/distance testi apex'e göre
                    Vector3 v = p - apex;
                    if (v.sqrMagnitude > rangeSqr) continue;

                    float dot = Vector3.Dot(dir, v.normalized); // cos(theta)
                    if (dot >= cosHalf)
                        detected.Add(actor);
                }
            }
            return detected;
        }
        
        public static void HitActorsInSphere(Vector3 center, float radius, LayerMask layerMask, HitInfo hitInfo,
            HashSet<int> factionFilter = null, UnityAction<Actor> damageHandler = null)
        {
            Collider[] results = UnityEngine.Physics.OverlapSphere(center, radius, layerMask.value);
            foreach (var result in results)
            {
                if(result == null) continue;
                if(!result.TryGetComponent(out Actor actor)) continue;
                if(!actor.IsAlive()) continue;
                int actorFaction = actor.GetFactionId();
                if(factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actorFaction)) continue;
                actor.OnHit(hitInfo);
                if (damageHandler != null)
                {
                    damageHandler(actor);
                }
            }
        }

        public static void HitActorsInCircle2D(Vector3 center, float range,
            LayerMask layerMask, HitInfo hitInfo, HashSet<int> factionFilter = null, UnityAction<Actor> damageHandler = null)
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(center, range, layerMask.value);
            foreach (var result in results)
            {
                if(result == null) continue;
                if(!result.TryGetComponent(out Actor actor)) continue;
                if(!actor.IsAlive()) continue;
                int actorFaction = actor.GetFactionId();
                if(factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actorFaction)) continue;
                actor.OnHit(hitInfo);
                if (damageHandler != null)
                {
                    damageHandler(actor);
                }
            }
        }
        public static void HitActorsInArc2D(Vector3 center, Vector3 direction, float range, float angle,
            LayerMask layerMask, HitInfo hitInfo, HashSet<int> factionFilter = null, UnityAction<Actor> damageHandler = null)
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(center, range, layerMask.value);
            foreach (var result in results)
            {
                if(result == null) continue;
                if(!result.TryGetComponent(out Actor actor)) continue;
                if(!actor.IsAlive()) continue;
                int actorFaction = actor.GetFactionId();
                if(factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actorFaction)) continue;
                
                //Check angle
                Vector3 toTarget = actor.transform.position - center;
                float angleTo = Vector2.Angle(direction, toTarget);
                if (angleTo <= angle * 0.5f)
                {
                    actor.OnHit(hitInfo);
                    if (damageHandler != null)
                    {
                        damageHandler(actor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Damages actors in a box
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="direction"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="layerMask"></param>
        /// <param name="hitInfo"></param>
        /// <param name="factionFilter">Factions to damage</param>
        /// <param name="damageHandler"></param>
        public static void HitActorsInBox2D(Vector3 startPosition, Vector3 direction, float width, float length, LayerMask layerMask,
            HitInfo hitInfo, HashSet<int> factionFilter = null, UnityAction<Actor> damageHandler = null)
        {
            List<Actor> actors = GetActorsInBox2D(startPosition, direction, width, length, layerMask, factionFilter);
            foreach (var actor in actors)
            {
                if (actor == null || !actor.IsAlive()) continue;
                if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.GetFactionId())) continue;
                
                actor.OnHit(hitInfo);
                // You can do something with hitInfo here if needed (like filling in contact point, etc.)
                damageHandler?.Invoke(actor);
            }
        }
        
        /// <summary>
        /// Gets actors in a 2d box
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="direction"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="layerMask"></param>
        /// <param name="factionFilter">Factions to get</param>
        /// <returns></returns>
        public static List<Actor> GetActorsInBox2D(Vector3 startPosition, Vector3 direction, float width, float length,
            LayerMask layerMask, HashSet<int> factionFilter = null)
        {
            List<Actor> actors = new List<Actor>();
            direction.z = 0;
            direction.Normalize();
            Vector3 boxCenter = startPosition + direction * length * 0.5f;
            // Get angle for the box rotation (only Z needed)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Size of the box (length along direction, width perpendicular)
            Vector2 boxSize = new Vector2(length, width);

            // Perform the box overlap
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, layerMask);
            if (hits.IsNullOrEmpty()) return actors;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive()) continue;
                if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.GetFactionId())) continue;
                
                actors.Add(actor);
            }

            return actors;
        }
        
        public static List<Actor> GetActorsInRaycast2D(Vector3 startPosition, Vector3 direction, float range,
            LayerMask layerMask, HashSet<int> factionFilter = null,
            UnityAction<Actor> damageHandler = null)
        {
            List<Actor> actors = new List<Actor>();
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction, range, layerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (!hit.collider.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive()) continue;
                if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.GetFactionId())) continue;
                actors.Add(actor);
            }

            return actors;
        }
        public static void HitActorsInRaycast2D(Vector3 startPosition, Vector3 direction, float range,
            LayerMask layerMask, HitInfo hitInfo, HashSet<int> factionFilter = null,
            UnityAction<Actor> damageHandler = null)
        {

            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction, range, layerMask);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (!hit.collider.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive()) continue;
                if (factionFilter != null && !factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.GetFactionId())) continue;

                actor.OnHit(hitInfo);
                damageHandler?.Invoke(actor);
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