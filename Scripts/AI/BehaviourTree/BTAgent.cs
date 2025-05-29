using System.Collections;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.AI
{
    public class BTAgent : ActorModule
    {
        private BehaviourTree Bt;
        [SerializeField] private BehaviourTreeBlueprint DefaultBtBlueprint;
        private WaitForSeconds _waitForSeconds;
        public bool AgentRunning;

        public override void Initialize()
        {
            //todo: Can we remove this?
            _waitForSeconds = new WaitForSeconds(0.01f);
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
            StartCoroutine(_behaveRoutine);
        }

        public void StopAgent()
        {
            AgentRunning = false;
            if(_behaveRoutine != null) StopCoroutine(_behaveRoutine);
            _behaveRoutine = null;
        }
        private IEnumerator Behave()
        {
            Bt.OwnerAgent = this;
            while (AgentRunning)
            {
                Bt.Process();
                yield return _waitForSeconds;
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