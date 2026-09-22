using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class TargetDetectionModule : ActorModule
    {
        public float DetectionRadius = 5.0f;
        public LayerMask TargetLayerMask;
        public bool AutoDetectTargets = true;
        public float AutoDetectFrequency;
        private float _lastDetectTime;
        public bool Is2D = true;
        
        [NonSerialized] public List<Actor> DetectedEnemies;
        [NonSerialized] public List<Actor> DetectedAllies;

        public TargetPriorityBehaviour allyTargetPriorityBehaviour;
        public TargetPriorityBehaviour EnemyDetectingBehaviour;

        // SubclassSelector, not a bare SerializeReference: Unity's own managed-reference type picker does
        // not produce a usable row here (the property measures as a normal line and then draws nothing),
        // so every polymorphic field in this project goes through SubclassSelectorDrawer instead.
        [SerializeReference] [SubclassSelector] public TargetFilter EnemyFilter = null;
        [SerializeReference] [SubclassSelector] public TargetFilter AllyFilter = null;

        /// <summary>
        /// Detects allies and enemies
        /// </summary>
        public void DetectTargets()
        {
            List<IHittable> hittables;
            if (Is2D)
            {
                hittables = CombatUtilities.GetHittablesInCircle2D(transform.position, DetectionRadius, TargetLayerMask);

            }
            else
            {
                hittables = CombatUtilities.GetHittablesInSphere(transform.position, DetectionRadius, TargetLayerMask);
            }

            DetectedAllies = new List<Actor>();
            DetectedEnemies = new List<Actor>();
            foreach (var hittable in hittables)
            {
                // This module tracks living Actor combatants only — a destructible prop or a corpse (still
                // technically hittable) is neither an ally nor an enemy to react to here.
                if (hittable is not Actor actor || actor == Actor || !actor.IsAlive()) continue;
                if (actor.IsAlly(Actor))
                {
                    //Is ally and not self
                    DetectedAllies.Add(actor);
                }
                else
                {
                    DetectedEnemies.Add(actor);
                }
            }
            SortActors();
            _lastDetectTime = Time.time;
        }
        
        /// <summary>
        /// Sets the candidate enemy targets for the actor.
        /// </summary>
        /// <param name="enemies"></param>
        public void SetEnemyTargets(List<Actor> enemies)
        {
            if (enemies.IsNullOrEmpty())
            {
                DetectedEnemies = new List<Actor>();
            }
            else
            {
                DetectedEnemies = new List<Actor>(enemies);
            }
        }
        
        public void SetAllyTargets(List<Actor> allies)
        {
            if (allies.IsNullOrEmpty())
            {
                DetectedAllies = new List<Actor>();
            }
            else
            {
                DetectedAllies = new List<Actor>(allies);
            }
        }
        
        /// <summary>
        /// Sorts actors depending on the targeting behaviour.
        /// </summary>
        public void SortActors()
        {
            //Sort enemies
            SortEnemies(EnemyDetectingBehaviour);

            //Sort allies
            SortAllies(allyTargetPriorityBehaviour);
        }
        
        public List<Actor> FilterEnemies()
        {
            if(EnemyFilter == null) return DetectedEnemies;
            List<Actor> filtered = new List<Actor>();
            foreach(var enemy in DetectedEnemies)
            {
                if(EnemyFilter.FilterOutTarget(Actor, enemy)) continue;
                filtered.Add(enemy);
            }
            return filtered;
        }

        public List<Actor> FilterAllies()
        {
            if (AllyFilter == null) return DetectedAllies;
            List<Actor> filtered = new List<Actor>();
            foreach (var ally in DetectedAllies)
            {
                if (AllyFilter.FilterOutTarget(Actor, ally)) continue;
                filtered.Add(ally);
            }
            return filtered;
        }

        public void SortEnemies(TargetPriorityBehaviour priorityBehaviour)
        {
            //Apply filter
            DetectedEnemies = FilterEnemies();
            if (!DetectedEnemies.IsNullOrEmpty() && DetectedEnemies.Count > 1 && priorityBehaviour != null)
            {
                DetectedEnemies.Sort((a, b) => priorityBehaviour.Compare(a, b, Actor));
            }
        }
        
        public void SortAllies(TargetPriorityBehaviour priorityBehaviour)
        {
            //Apply Filter
            DetectedAllies = FilterAllies();
            if (!DetectedAllies.IsNullOrEmpty() && DetectedAllies.Count > 1 && priorityBehaviour != null)
            {
                DetectedAllies.Sort((a, b) => priorityBehaviour.Compare(a, b, Actor));
            }
        }
        
        /// <summary>
        /// Gets enemy without touching original enemies list
        /// </summary>
        /// <param name="priorityBehaviour"></param>
        /// <returns></returns>
        public Actor GetEnemyByTargetPriority(TargetPriorityBehaviour priorityBehaviour)
        {
            if (DetectedEnemies.IsNullOrEmpty() || priorityBehaviour == null) return null;
            List<Actor> sortedEnemies = new List<Actor>(DetectedEnemies);
            sortedEnemies.Sort((a, b) => priorityBehaviour.Compare(a, b, Actor));
            return sortedEnemies[0];
        }
        
        /// <summary>
        /// Gets ally without touching original ally list
        /// </summary>
        /// <param name="priorityBehaviour"></param>
        /// <returns></returns>
        public Actor GetAllyByTargetPriority(TargetPriorityBehaviour priorityBehaviour)
        {
            if (DetectedAllies.IsNullOrEmpty() || priorityBehaviour == null) return null;
            List<Actor> sortedAllies = new List<Actor>(DetectedAllies);
            sortedAllies.Sort((a, b) => priorityBehaviour.Compare(a, b, Actor));
            return sortedAllies[0];
        }

        public static Actor SortActorsByPriority(Actor self, List<Actor> actors, TargetPriorityBehaviour priorityBehaviour)
        {
            if (actors.IsNullOrEmpty() || priorityBehaviour == null) return null;
            actors.Sort((a, b) => priorityBehaviour.Compare(a, b, self));
            return actors[0];
        }
        
        /// <summary>
        /// Returns the first enemy target
        /// </summary>
        /// <returns></returns>
        public Actor GetEnemyTarget()
        {
            if (DetectedEnemies.IsNullOrEmpty()) return null;
            foreach (var enemy in DetectedEnemies)
            {
                if (!enemy.IsAlive())
                {
                    continue;
                }

                return enemy;
            }
            
            return null;
        }
        
        /// <summary>
        /// Returns the first ally target.
        /// </summary>
        /// <returns></returns>
        public Actor GetAllyTarget()
        {
            if (DetectedAllies.IsNullOrEmpty()) return null;
            foreach (var ally in DetectedAllies)
            {
                if (!ally.IsAlive())
                {
                    Debug.LogError("Dead ally in detected allies list");
                    continue;
                }

                return ally;
            }
            return null;
        }
        
        // Was a Unity Update message. Every ActorModule that keeps one is ticked by the engine for
        // every instance every frame, which is both the per-component overhead ActorManager exists to
        // remove and a way out of the update rate the actor asked for.
        public override void ModuleUpdate(float deltaTime)
        {
            base.ModuleUpdate(deltaTime);
            if (!Initialized || !AutoDetectTargets || Actor.CurrentActorState != ActorState.Spawned) return;
            float elapsedTime = Time.time - _lastDetectTime;
            if (elapsedTime < AutoDetectFrequency) return;
            DetectTargets();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if(DetectedEnemies != null) DetectedEnemies.Clear();
            if(DetectedAllies != null) DetectedAllies.Clear();
        }
    }
}