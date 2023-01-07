using System;
using Kuantech.Core;
using Kuantech.EndlessRunner;
using UnityEngine;

namespace Kuantech.Managers
{
    public enum EnemyAIStates
    {
        Idle = 0,
        Following,
        Attacking,
    }
    
    public class EnemyAIManager : Singleton<EnemyAIManager>
    {
        [Header("Surround System")] 
        public int SurroundSystemColumnCount = 5;
        public float SurroundSystemVerticalOffset;
        public float SurroundSystemHorizontalOffset;
        public SurroundSystem.SurroundSystem SurroundSystem;
        public static float EnemyFollowThreshold = 0.1f;

        private void Awake()
        {
            SurroundSystem = new SurroundSystem.SurroundSystem(LooterGameManager.Instance.Player.transform);
            SurroundSystem.RowSlotCount = SurroundSystemColumnCount;
            SurroundSystem.HorizontalDistance = SurroundSystemHorizontalOffset;
            SurroundSystem.VerticalDistance = SurroundSystemVerticalOffset;
        }

        private void Start()
        {
            LooterGameManager.Instance.StartLevelEvent += OnLevelReset;
            LooterGameManager.Instance.StateChangeEvent += OnStateChange;
        }

        private void FixedUpdate()
        {
            SurroundSystem?.HandleAgentQueue();
            
            //Get players horizontal position
            SurroundSystem?.SetHorizontalOffsets(15f, LooterGameManager.Instance.Player.transform.position.x/7.5f);
        }

        private void Update()
        {
            //SurroundSystem.RecalculateSlots();
        }

        private void OnLevelReset(object sender, EventArgs args)
        {
            SurroundSystem.Cleanup();
        }
        
        private void OnStateChange(object sender, LevelState newState)
        {
            SurroundSystem.Cleanup();
        }
    }
}