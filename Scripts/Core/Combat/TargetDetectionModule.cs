using System;
using System.Collections.Generic;
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
        
        /// <summary>
        /// Detects allies and enemies
        /// </summary>
        public void DetectTargets()
        {
            List<Actor> actors = CombatUtilities.GetActorsInRange(transform.position, DetectionRadius, TargetLayerMask);
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
            _lastDetectTime = Time.time;
        }

   
        private void Update()
        {
            if (!Initialized || !AutoDetectTargets) return;
            float elapsedTime = Time.time - _lastDetectTime;
            if (elapsedTime < AutoDetectFrequency) return;
            DetectTargets();
        }
    }
}