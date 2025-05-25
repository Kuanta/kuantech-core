using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public static class CombatUtilities
    {
        public static List<Actor> GetActorsInRange(Vector3 position, float radius, LayerMask layerMask, string[] allowedTags = null)
        {
            Collider[] hits = UnityEngine.Physics.OverlapSphere(position, radius, layerMask);
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
        
    }
}