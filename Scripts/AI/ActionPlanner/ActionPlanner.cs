using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI
{
    public class ActionPlanner : Kuantech.Core.ActorModule
    {
        [SerializeField] private List<ActionEntry> Entries = new();

        public Action CurrentAction { get; private set; }

        /// <summary>
        /// Evaluates all entries and activates the highest-priority valid one.
        /// Returns false if no entry passes conditions and cooldown.
        /// Called by the BT DecideAction node.
        /// </summary>
        public bool TrySelectAction()
        {
            ActionEntry best = null;
            float bestPriority = float.MinValue;

            foreach (var entry in Entries)
            {
                if (entry.Asset == null) continue;
                if (!entry.IsReady()) continue;
                if (!entry.EvaluateConditions(Actor)) continue;
                if (entry.Priority > bestPriority)
                {
                    bestPriority = entry.Priority;
                    best = entry;
                }
            }

            if (best == null) return false;

            ActivateEntry(best);
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
