using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech.AI
{
    public class BTAgent : ActorModule
    {
        private BehaviourTree Bt;
        [SerializeField] private float TickInterval = 0.1f;
        [SerializeField] private float TickJitter = 0.05f;
        [SerializeField] private BehaviourTreeBlueprint DefaultBtBlueprint;
        private WaitForSeconds _waitForSeconds;
        [NonSerialized] public bool AgentRunning;
        [NonSerialized] public bool AgentPaused;
        
        private float _nextPlanTime;
        
        public override void Initialize()
        {
            //todo: Can we remove this?
            _waitForSeconds = new WaitForSeconds(TickInterval);
            if (DefaultBtBlueprint == null) return;
            SetBehaviourTree(DefaultBtBlueprint.CreateBehaviourTree());
        }

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if (oldState == ActorState.Spawned && newState != ActorState.Spawned)
            {
                StopAgent();
            }
            else if (oldState != ActorState.Spawned && newState == ActorState.Spawned)
            {
                StartAgent();
            }
        }

        #region Behaviour Tree

        public void SetBehaviourTree(BehaviourTree bt)
        {
            bt.VariableTable ??= new BTVariableTable();
            bt.VariableTable.ClearTable();
            bt.OwnerAgent = this;
            Bt = bt;
        }
        
        public BehaviourTree GetBehaviourTree()
        {
            return Bt;
        }

        private IEnumerator _behaveRoutine = null;
        public void StartAgent()
        {
            if(_behaveRoutine != null) StopCoroutine(_behaveRoutine);
            _behaveRoutine = Behave();
            AgentRunning = true;
            AgentPaused = false;
            StartCoroutine(_behaveRoutine);
        }

        public void ResumeAgent()
        {
            AgentPaused = false;
        }
        public void PauseAgent()
        {
            AgentPaused = true;
        }
        public void StopAgent()
        {
            AgentRunning = false;
            if(_behaveRoutine != null) StopCoroutine(_behaveRoutine);
            _behaveRoutine = null;
        }
        private IEnumerator Behave()
        {
            if (Bt == null) yield break;

            float firstDelay = (TickJitter > 0f) ? Random.Range(0f, TickJitter) : 0f;
            if (firstDelay > 0f) yield return new WaitForSeconds(firstDelay);

            Bt.OwnerAgent = this; 
            bool shouldExecuteImmediately = false;

            while (AgentRunning)
            {
                if (!AgentPaused)
                {
                    shouldExecuteImmediately |= Bt.ShouldExecuteNodeImmediately();
                    Bt.Process();         
                }

                if (!shouldExecuteImmediately)
                {
                    yield return _waitForSeconds; 
                }
                else
                {
                    yield return null; //next frame
                }
            }
        }

        public void RegisterVariable(string key, object variable)
        {
            Bt.VariableTable ??= new BTVariableTable();
            Bt.VariableTable.RegisterVariable(key, variable);
        }


        #endregion
       
        public override void Reset()
        {
            StopAgent();
        }
    }
}