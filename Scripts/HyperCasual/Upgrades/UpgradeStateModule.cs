using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HyperCasual
{
    [CreateAssetMenu(fileName = "Upgrades State Module", menuName = "Kuantech/StateModules/UpgradeStateModule")]
    public class UpgradeStateModule : StateModule
    {
        public override string ModuleID => typeof(UpgradeStateModule).ToString();
        public Dictionary<string, int> UpgradeLevels;
        public override void SetDefaultValues()
        {
            UpgradeLevels = new Dictionary<string, int>();
        }

        public void SetUpgradeLevel(string boostId, int level)
        {
            if (UpgradeLevels == null) UpgradeLevels = new Dictionary<string, int>();
            UpgradeLevels[boostId] = level;
            Dirtied = true;
        }

        public int GetUpgradeLevel(string boostId)
        {
            if (UpgradeLevels == null || !UpgradeLevels.ContainsKey(boostId)) return 0;
            return UpgradeLevels[boostId];
        }

        public override object GetData()
        {
            throw new System.NotImplementedException();
        }

        public override void SetData(object loadedData)
        {
            throw new System.NotImplementedException();
        }

        public override Type GetDataType()
        {
            throw new NotImplementedException();
        }
    }
}