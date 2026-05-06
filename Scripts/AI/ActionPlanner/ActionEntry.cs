using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.AI
{
    [Serializable]
    public class ActionEntry
    {
        public ActionAsset Asset;
        public float Priority;
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

        public void RecordExecution() => _lastExecutedTime = Time.time;

        public void ResetCooldown() => _lastExecutedTime = float.MinValue;
    }
}
