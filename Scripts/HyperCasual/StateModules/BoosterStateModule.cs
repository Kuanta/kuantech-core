using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [CreateAssetMenu(fileName = "Booster State Module", menuName = "Kuantech/StateModules/BoosterStateModule")]
    public class BoosterStateModule : StateModule
    {
        public override string ModuleID => typeof(BoosterStateModule).ToString();
        public Dictionary<string, int> BoostLevels;
        public override void SetDefaultValues()
        {
            BoostLevels = new Dictionary<string, int>();
        }

        public void SetBoostLevel(string boostId, int level)
        {
            if (BoostLevels == null) BoostLevels = new Dictionary<string, int>();
            BoostLevels[boostId] = level;
            Dirtied = true;
        }

        public int GetBoostLevel(string boostId)
        {
            if (BoostLevels == null || !BoostLevels.ContainsKey(boostId)) return 0;
            return BoostLevels[boostId];
        }
    }
}