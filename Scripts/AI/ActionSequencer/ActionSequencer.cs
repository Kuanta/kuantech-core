using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.ActionSequencer
{
    public enum ActionTypes
    {
        None=0,
        Move,
        Attack,
    }
    
    public class ActionSequencer : MonoBehaviour
    {
        [SerializeReference] public List<SequenceAction> Sequence = new List<SequenceAction>();
        [ValueDropdown("Actions")]
        [OnValueChanged("AddAction")]
        public ActionTypes ActionDropdown;
        private static IEnumerable<ActionTypes> Actions = Enumerable.Range((int)ActionTypes.None, 3).Cast<ActionTypes>();
        
        private int _currentSequenceIndex = 0;
        private bool _sequenceStarted = false;
        
        private void Start()
        {
            _currentSequenceIndex = 0;
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
        
        private void SetNextSequence()
        {
            _currentSequenceIndex++;
            if (_currentSequenceIndex >= Sequence.Count)
            {
                _currentSequenceIndex = 0;
            }
            Sequence[_currentSequenceIndex].Execute();
        }
        
        [Button(ButtonSizes.Large)]
        public void AddAction()
        {
            switch (ActionDropdown)
            {
                case ActionTypes.None:
                    break;
                case ActionTypes.Move:
                    MoveAction ma = new MoveAction();
                    ma.Parent = gameObject;
                    Sequence.Add(ma);
                    break;
                case ActionTypes.Attack:
                    AttackAction aa = new AttackAction();
                    aa.Parent = gameObject;
                    aa.CombatModule = GetComponent<CombatModule>();
                    Sequence.Add(aa);
                    break;
            }
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
    }
}