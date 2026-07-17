using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Rpg
{
    public class LootVacuumModule : ActorModule
    {
        [Header("Vacuum")]
        public AttributeBasedVariable VacuumRange;
        public AttributeBasedVariable VacuumSpeed;
        public LayerMask DropObjectLayer;

        [Header("Timing")]
        [SerializeField] private float _checkInterval = 0.1f;

        private StatsModule _statsModule;
        private float _checkTimer;
        private readonly Collider[] _overlapBuffer = new Collider[32];

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _statsModule = Actor.GetModule<StatsModule>();
        }

        public override void ModuleUpdate()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < _checkInterval) return;
            _checkTimer = 0f;
            PullNearbyDrops();
        }

        private void PullNearbyDrops()
        {
            float range = VacuumRange.GetValue(_statsModule);
            float speed = VacuumSpeed.GetValue(_statsModule);

            int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, range, _overlapBuffer, DropObjectLayer);
            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i].TryGetComponent(out DropObject drop))
                    drop.BeginVacuum(this, speed);
            }
        }
    }
}
