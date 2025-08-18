using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.RealTimeStrategy;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class ActorSummoner : LevelElement
    {
        public int ActorFactionId = 0;
        public List<Transform> SpawnPoints;
        public bool Toggled = true;
        public Vector3 TargetVector;
     
        
        private UnitsManager _unitsManager;
        public override void OnPrePlayLevel()
        {
            base.OnPrePlayLevel();
            _unitsManager = ParentLevel.GetLevelModule<UnitsManager>();
        }
        public virtual Actor SpawnActor(ActorBlueprint actorTemplate, int order = 0)
        {
            if (actorTemplate == null || !Toggled) return null;
            Actor createdActor = _unitsManager == null ? actorTemplate.CreateActor() :
                _unitsManager.SpawnActor(actorTemplate);
            if (createdActor == null) return null;
            createdActor.Spawn();
            createdActor.SetFactionId(ActorFactionId);


            createdActor.transform.position = GetSpawnPoint(order).position;
            if (TargetVector.sqrMagnitude > 0.1f)
            {
                createdActor.MotionVectorsHandler.SetTargetVector(TargetVector);
            }
            return createdActor;
        }

        private Transform GetSpawnPoint(int order)
        {
            if (SpawnPoints.IsNullOrEmpty()) return transform;
            return SpawnPoints[order % SpawnPoints.Count];
        }
    }
}