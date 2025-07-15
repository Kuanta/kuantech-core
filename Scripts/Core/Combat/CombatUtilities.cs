using System.Collections.Generic;
using Kuantech.Utils;
using Unity.VisualScripting;
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
        /// <param name="allowedTags"></param>
        /// <returns></returns>
        public static List<Actor> GetActorsInCircle2D(Vector3 position, float radius, LayerMask layerMask, string[] allowedTags = null)
        {
            Collider2D[] hits = UnityEngine.Physics2D.OverlapCircleAll(position, radius, layerMask);
           // Collider[] hits = UnityEngine.Physics.OverlapSphere(position, radius, layerMask);
            List<Actor> actors = new();

            foreach (var hit in hits)
            {
                Actor actor = hit.GetComponentInParent<Actor>();
                if (actor == null) continue;
                if (allowedTags != null && allowedTags.Length > 0)
                {
                    bool tagMatch = false;
                    foreach (string tag in allowedTags)
                    {
                        if (actor.CompareTag(tag))
                        {
                            tagMatch = true;
                            break;
                        }
                    }
                    if (!tagMatch) continue;
                }

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


        public static void HidActorsInCircle2D(Vector3 center, float range,
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
        

        #endregion
       
    }
}