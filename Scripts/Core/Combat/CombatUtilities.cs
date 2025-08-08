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
                if(factionFilter != null && factionFilter.Contains(actor.FactionId)) continue;
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
        /// <param name="factionFilter"></param>
        /// <returns></returns>
        public static List<Actor> GetActorsInArc2D(Vector3 center, Vector3 direction, float range, float angle,
            LayerMask layerMask, HashSet<int> factionFilter = null)
        {
            List<Actor> detectedActors = new List<Actor>();
            Collider2D[] results = Physics2D.OverlapCircleAll(center, range, layerMask);
            foreach (var result in results)
            {
                if(result == null) continue;
                if(!result.TryGetComponent(out Actor actor)) continue;
                if(!actor.IsAlive()) continue;
                int actorFaction = actor.FactionId;
                if(!factionFilter.IsNullOrEmpty() && factionFilter.Contains(actorFaction)) continue;
                
                //Check angle
                Vector3 toTarget = actor.transform.position - center;
                float angleTo = Vector2.Angle(direction, toTarget);
                if (angleTo <= angle * 0.5f)
                {
                    detectedActors.Add(actor);
                }
            }

            return detectedActors;
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
                int actorFaction = actor.FactionId;
                if(!factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actorFaction)) continue;
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
                int actorFaction = actor.FactionId;
                if(!factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actorFaction)) continue;
                
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


        public static void HitActorsInBox2D(Vector3 startPosition, Vector3 direction, float width, float length, LayerMask layerMask,
            HitInfo hitInfo, HashSet<int> factionFilter = null, UnityAction<Actor> damageHandler = null)
        {
            direction.z = 0;
            direction.Normalize();
            Vector3 boxCenter = startPosition + direction * length * 0.5f;
            // Get angle for the box rotation (only Z needed)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Size of the box (length along direction, width perpendicular)
            Vector2 boxSize = new Vector2(length, width);

            // Perform the box overlap
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, layerMask);

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive()) continue;
                if (!factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.FactionId)) continue;
                
                actor.OnHit(hitInfo);
                // You can do something with hitInfo here if needed (like filling in contact point, etc.)
                damageHandler?.Invoke(actor);
            }
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
                if (!factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.FactionId)) continue;
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
                if (!factionFilter.IsNullOrEmpty() && !factionFilter.Contains(actor.FactionId)) continue;

                actor.OnHit(hitInfo);
                damageHandler?.Invoke(actor);
            }
        }
        #endregion
       
    }
}