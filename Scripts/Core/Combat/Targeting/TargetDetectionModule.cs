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
        
        /// <summary>
        /// Detects allies and enemies
        /// </summary>
        public void DetectTargets()
        {
            List<Actor> actors;
            if (Is2D)
            {
                actors = CombatUtilities.GetActorsInCircle2D(transform.position, DetectionRadius, TargetLayerMask);

            }
            else
            {
                actors = CombatUtilities.GetActorsInSphere(transform.position, DetectionRadius, TargetLayerMask);
            }
            
            DetectedAllies = new List<Actor>();
            DetectedEnemies = new List<Actor>();
            foreach (var actor in actors)
            {
                if(actor == Actor || !actor.IsAlive()) continue;
                if (actor.IsAlly(Actor) && actor != Actor) 
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
        
        public void SortEnemies(TargetPriorityBehaviour priorityBehaviour)
        {
            if (!DetectedEnemies.IsNullOrEmpty() && DetectedEnemies.Count > 1 && priorityBehaviour != null)
            {
                DetectedEnemies.Sort((a, b) => priorityBehaviour.Compare(a, b, Actor));
            }
        }
        
        public void SortAllies(TargetPriorityBehaviour priorityBehaviour)
        {
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
                    Debug.LogError("Dead enemy in DetectedEnemies list");
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
        
        private void Update()
        {
            if (!Initialized || !AutoDetectTargets || Actor.CurrentActorState != ActorState.Spawned) return;
            float elapsedTime = Time.time - _lastDetectTime;
            if (elapsedTime < AutoDetectFrequency) return;
            DetectTargets();
        }

        public override void Reset()
        {
            base.Reset();
            if(DetectedEnemies != null) DetectedEnemies.Clear();
            if(DetectedAllies != null) DetectedAllies.Clear();
        }
    }
}