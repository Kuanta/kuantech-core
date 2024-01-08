using System;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
   
    [Serializable]
    public struct ResourceNodeState
    {
        public int CurrentResourceAmount;
        public float TimeSinceLastResourceSpawn;
    }
    public class ResourceNode : MonoBehaviour
    {
        public ResourceData ResourceData;
        
        [Header("Properties")]
        public float RefreshRate = 2f;
        public int RefreshAmount = 1;
        public int MaxAmount = 5;
        public float HandoverPeriod = 0.1f;
        public int HandoverAmount;
        private float _lastHandoverTime;
        
        [Header("Components")] 
        public CollisionEventsRelayer CollisionEventsRelayer;
        public ResourceNodeState CurrentState;
               

        private GameObject _currentResourceCollectorGameobject = null;
        private IResourceCollector _currentResourceCollector = null;

        private void Start()
        {
            //todo: Temporary
            Initialize();
        }

        public void Initialize()
        {
            Initialize(new ResourceNodeState()
            {
                CurrentResourceAmount = 0,
                TimeSinceLastResourceSpawn = 0f,
            });
        }
        public void Initialize(ResourceNodeState resourceNodeState)
        {
            CurrentState = resourceNodeState;
            for (int i = 0; i < resourceNodeState.CurrentResourceAmount; ++i)
            {
                GenerateResource();
            }
            _currentResourceCollector = null;
            _currentResourceCollectorGameobject = null;
            CollisionEventsRelayer.OnTriggerEnterEvent += (sender, collision) =>
            {
                if (collision.gameObject.TryGetComponent(out IResourceCollector rc))
                {
                    _currentResourceCollector = rc;
                    _currentResourceCollectorGameobject = collision.gameObject;
                }
            };
            
            CollisionEventsRelayer.OnTriggerExitEvent += (sender, collision) =>
            {
                if (!collision.gameObject.TryGetComponent(out IResourceCollector rc)) return;
                if (rc == _currentResourceCollector)
                {
                    _currentResourceCollector = null;
                    _currentResourceCollectorGameobject = null;
                }
            };
        }
        
        private void Update()
        {
            CheckResourceGeneration();
            CheckResourceHandover();
        }
        
        /// <summary>
        /// Checks if a new resource should be generated
        /// </summary>
        private void CheckResourceGeneration()
        {
            if (CurrentState.CurrentResourceAmount >= MaxAmount) return;
            if (CurrentState.TimeSinceLastResourceSpawn > RefreshRate)
            {
                GenerateResource();
                CurrentState.TimeSinceLastResourceSpawn = 0f;
                return;
            }
            CurrentState.TimeSinceLastResourceSpawn += Time.deltaTime;
        }

        private void CheckResourceHandover()
        {
            if (_currentResourceCollector == null) return;
            if (!(Time.time - _lastHandoverTime > HandoverPeriod)) return;
            HandoverResource(_currentResourceCollector);
            _lastHandoverTime = Time.time;
        }
        
        /// <summary>
        /// Generates a new resource
        /// </summary>
        private void GenerateResource()
        {
            CurrentState.CurrentResourceAmount += RefreshAmount;
        }
        
        /// <summary>
        /// Gives a resource to a collector
        /// </summary>
        public void HandoverResource(IResourceCollector collector)
        {
            if (CurrentState.CurrentResourceAmount <= 0 || !collector.CanCollectResource(ResourceData.ResourceId)) return;
            int currentAmount = CurrentState.CurrentResourceAmount;
            int removedAmount = Mathf.Min( HandoverAmount, currentAmount);
            collector.CollectResource(ResourceData.ResourceId, removedAmount);
            CurrentState.CurrentResourceAmount -= removedAmount;
        }

     
    }
}