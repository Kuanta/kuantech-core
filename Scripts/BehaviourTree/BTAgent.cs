using System.Collections;
using UnityEngine;

namespace Kuantech.AI
{
    public class BTAgent : MonoBehaviour
    {
        private BehaviourTree Bt;
        private WaitForSeconds _waitForSeconds;
        public bool AgentRunning;
        public void Initialize()
        {
            _waitForSeconds = new WaitForSeconds(Random.Range(0.1f, 1f));
        }

        public void SetBehaviourTree(BehaviourTree bt)
        {
            bt.VariableTable ??= new BTVariableTable();
            bt.VariableTable.ClearTable();
            bt.OwnerAgent = this;
            Bt = bt;
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
        
    }
}