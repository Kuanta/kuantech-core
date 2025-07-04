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
        
        [NonSerialized] public List<Actor> DetectedEnemies;
        [NonSerialized] public List<Actor> DetectedAllies;

        public TargetPriorityBehaviour allyTargetPriorityBehaviour;
        public TargetPriorityBehaviour EnemyDetectingBehaviour;
        
        /// <summary>
        /// Detects allies and enemies
        /// </summary>
        public void DetectTargets()
        {
            List<Actor> actors = CombatUtilities.GetActorsInRange(transform.position, DetectionRadius, TargetLayerMask);
            DetectedAllies = new List<Actor>();
            DetectedEnemies = new List<Actor>();
            foreach (var actor in actors)
            {
                if(actor == Actor || !actor.IsAlive()) continue;
                if (actor.FactionId == Actor.FactionId)
                {
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
            if (!DetectedEnemies.IsNullOrEmpty() && DetectedEnemies.Count > 1 && EnemyDetectingBehaviour != null)
            {
                DetectedEnemies.Sort((a, b) => EnemyDetectingBehaviour.Compare(a, b, Actor));
            }
            
            //Sort allies
            if (!DetectedAllies.IsNullOrEmpty() && DetectedAllies.Count > 1 && allyTargetPriorityBehaviour != null)
            {
                DetectedAllies.Sort((a,b)=>allyTargetPriorityBehaviour.Compare(a, b, Actor));
            }
            
        }
        
        /// <summary>
        /// Returns the first enemy target
        /// </summary>
        /// <returns></returns>
        public Actor GetEnemyTarget()
        {
            if (DetectedEnemies.IsNullOrEmpty()) return null;
            return DetectedEnemies[0];
        }
        
        /// <summary>
        /// Returns the first ally target.
        /// </summary>
        /// <returns></returns>
        public Actor GetAllyTarget()
        {
            if (DetectedAllies.IsNullOrEmpty()) return null;
            return DetectedAllies[0];
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