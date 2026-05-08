using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.AI
{
    public class ActionPlanner : ActorModule
    {
        [SerializeField] private List<ActionEntry> Entries = new();

        
        //Runtime
        public Action CurrentAction { get; private set; }
        private PriorityBasedSelector<ActionEntry> _selector = new PriorityBasedSelector<ActionEntry>();
        public float Cooldown = 1.0f;
        private float _lastActionDecidedTime;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            
            _selector = new PriorityBasedSelector<ActionEntry>();
            foreach(var entry in Entries)
            {
                _selector.AddElement(new PriorityBasedSelector<ActionEntry>.PriorityBasedSelectorElement
                {
                    Element = entry,
                    Priority = entry.Priority,
                    Probability = entry.Probability,
                }, false);
            }

            _selector.RebuildElements();
        }

        /// <summary>
        /// Evaluates all entries and activates the highest-priority valid one.
        /// Returns false if no entry passes conditions and cooldown.
        /// Called by the BT DecideAction node.
        /// </summary>
        public bool TrySelectAction()
        {
            if(Time.time - _lastActionDecidedTime < Cooldown) return false;
            ActionEntry selectedEntry = _selector.SelectElement(Actor);
            if (selectedEntry == null) return false;
            ActivateEntry(selectedEntry);
            _lastActionDecidedTime = Time.time;
            return true;
        }

        /// <summary>
        /// Ticks the current action. Returns the action's state.
        /// Called by the BT ExecuteAction node each frame.
        /// </summary>
        public Action.ActionState TickCurrentAction(float deltaTime)
        {
            if (CurrentAction == null) return Action.ActionState.SUCCESS;

            Action.ActionState state = CurrentAction.OnUpdate(deltaTime);
            if (state != Action.ActionState.RUNNING)
            {
                CurrentAction.OnExit();
                CurrentAction = null;
            }
            return state;
        }

        /// <summary>Interrupts and clears the running action.</summary>
        public void InterruptCurrentAction()
        {
            if (CurrentAction == null) return;
            CurrentAction.OnInterrupt();
            CurrentAction = null;
        }

        public bool HasCurrentAction() => CurrentAction != null;

        public override void ResetModule()
        {
            base.ResetModule();
            InterruptCurrentAction();
            foreach (var entry in Entries)
                entry.ResetCooldown();
        }

        private void ActivateEntry(ActionEntry entry)
        {
            CurrentAction?.OnExit();
            CurrentAction = entry.Asset.CreateAction(Actor);
            entry.RecordExecution();
            CurrentAction.OnEnter();
        }
    }
}
