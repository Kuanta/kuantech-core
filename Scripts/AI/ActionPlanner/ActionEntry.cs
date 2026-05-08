using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.AI
{
    [Serializable]
    public class ActionEntry : IPriorityBasedSelectorElement
    {
        public ActionAsset Asset;
        public int Priority;
        public float Probability;
        public float Cooldown;
        [SerializeReference] public List<ActorCondition> Conditions = new();

        [NonSerialized] private float _lastExecutedTime = float.MinValue;

        public bool IsReady() => Time.time - _lastExecutedTime >= Cooldown;

        public bool EvaluateConditions(Actor owner)
        {
            if(Conditions.IsNullOrEmpty()) return true;
            foreach (var c in Conditions)
                if (!c.IsSatisfied(owner)) return false;
            return true;
        }

        //Checks both readiness and conditions
        public bool CanBeActivated(Actor owner)
        {
            return IsReady() && EvaluateConditions(owner);
        }

        public void RecordExecution() => _lastExecutedTime = Time.time;

        public void ResetCooldown() => _lastExecutedTime = float.MinValue;

        public bool CanBeSelected(object userData = null)
        {
            if(userData is Actor owner)
            {
                return CanBeActivated(owner);
            }
            return IsReady();
        }

    }
}
