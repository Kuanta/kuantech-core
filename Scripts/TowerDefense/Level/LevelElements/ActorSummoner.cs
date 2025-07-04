using Kuantech.Core;
using Kuantech.RealTimeStrategy;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class ActorSummoner : LevelElement
    {
        public int ActorFactionId = 0;
        public Transform SpawnPoint;
        public bool Toggled = true;
        
        private UnitsManager _unitsManager;
        public override void OnPrePlayLevel()
        {
            base.OnPrePlayLevel();
            _unitsManager = ParentLevel.GetLevelModule<UnitsManager>();
        }
        public virtual Actor SpawnActor(ActorBlueprint actorTemplate)
        {
            if (actorTemplate == null || !Toggled) return null;
            Actor createdActor = _unitsManager == null ? actorTemplate.CreateActor() :
                _unitsManager.SpawnActor(actorTemplate);
            createdActor.Spawn();
            createdActor.FactionId = ActorFactionId;
            createdActor.transform.position = SpawnPoint.position;
            return createdActor;
        }
    }
}