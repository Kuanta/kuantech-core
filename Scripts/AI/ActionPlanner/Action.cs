using Kuantech.Core;

namespace Kuantech.AI
{
    public abstract class Action
    {
        public enum ActionState {RUNNING, SUCCESS, FAILED}
        public Actor Owner { get; internal set; }

        public virtual void OnEnter() { }

        /// <summary>Returns true when the action is complete.</summary>
        public abstract ActionState OnUpdate(float deltaTime);

        public virtual void OnExit() { }

        /// <summary>Called when the planner interrupts a running action.</summary>
        public virtual void OnInterrupt() => OnExit();

        public virtual Action Clone() => (Action)MemberwiseClone();
    }
}
