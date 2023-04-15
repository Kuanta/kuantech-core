using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.AI.ActionSequencer;
using Kuantech.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.ActionSequencer
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
                    ma.Sequencer = this;
                    break;
                case ActionTypes.Attack:
                    AttackAction aa = new AttackAction(AttackTypes.Linear, true);
                    aa.Parent = gameObject;
                    aa.CombatModule = GetComponent<CombatModule>();
                    Sequence.Add(aa);
                    aa.Sequencer = this;
                    break;
                case ActionTypes.Delay:
                    DelayAction da = new DelayAction();
                    da.Parent = gameObject;
                    Sequence.Add(da);
                    da.Sequencer = this;
                    break;
                case ActionTypes.Rotate:
                    RotateAction ra = new RotateAction();
                    ra.Parent = gameObject;
                    Sequence.Add(ra);
                    ra.Sequencer = this;
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

        public void ClearActions()
        {
            Sequence.Clear();
            _currentSequenceIndex = 0;
        }
    }
}