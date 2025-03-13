using System.Collections.Generic;
using Kuantech.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.AI.ActionSequencer
{
    public enum ActionTypes
    {
        None=0,
        Move,
        Attack,
        Delay,
        Rotate,
    }
    
    public class ActionSequencer : MonoBehaviour
    {
        
        [SerializeReference] public List<SequenceAction> Sequence = new List<SequenceAction>();
        [ValueDropdown("Actions")]
        [OnValueChanged("AddAction")]
        public ActionTypes ActionDropdown;
        private int _currentSequenceIndex = 0;
        private bool _sequenceStarted = false;

        public VariableTable VariableTable = new VariableTable();
        private void Start()
        {
           Initialize();
        }

        public void Initialize()
        {
            _currentSequenceIndex = 0;
            foreach (var action in Sequence)
            {
                action.Initialize(gameObject);
                action.Sequencer = this;
            }
        }
        private void Update()
        {
            if (Sequence == null || Sequence.Count == 0 || !_sequenceStarted) return;
            SequenceAction current = Sequence[_currentSequenceIndex];
            if (current.IsComplete)
            {
                current.Terminate();
                SetNextSequence();
            }
            else
            {
                current.Update(Time.deltaTime);
            }

        }

        public void StartSequence()
        {
            _sequenceStarted = true;
            _currentSequenceIndex = 0;
        }

        public void StopSequence()
        {
            _sequenceStarted = false;
        }

        public bool IsSequenceStarted()
        {
            return _sequenceStarted;
        }
        
        private void SetNextSequence()
        {
            _currentSequenceIndex++;
            if (_currentSequenceIndex >= Sequence.Count)
            {
                _currentSequenceIndex = 0;
            }
            Sequence[_currentSequenceIndex].Execute();
        }
        public void Reset()
        {
            if (Sequence == null || Sequence.Count == 0) return;
            foreach (var action in Sequence)
            {
                action.IsComplete = false;
            }
            SequenceAction current = Sequence[_currentSequenceIndex];
            current.Terminate();
            _currentSequenceIndex = 0;
            _sequenceStarted = false;
        }

        public void ClearActions()
        {
            Sequence.Clear();
            _currentSequenceIndex = 0;
        }
    }
}