using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class HyperCasualGameModelData
    {
        public int LevelIndex;
        //public Dictionary<string, Currency> Currencies;
    }


    [CreateAssetMenu(menuName = "Kuantech/StateModules/HyperCasualModule")]
    public class HyperCasualGameModel : StateModule
    {
        // A unique identifier for this module.
        public override string ModuleID => typeof(HyperCasualGameModel).ToString();

       
        public HyperCasualGameModelData Data;

        public override void SetDefaultValues()
        {
            Data = new HyperCasualGameModelData();
            Data.LevelIndex = 0;
            //Data.Currencies = new Dictionary<string, Currency>();
        }

        public void SetLevelIndex(int levelIndex)
        {
            Data.LevelIndex = levelIndex;
            Dirtied = true;
        }

        public int GetLevelIndex()
        {
            return Data.LevelIndex;
        }

        public override object GetData()
        {
            return Data;
        }

        public override void SetData(object loadedData)
        {
            Data = loadedData as HyperCasualGameModelData;
        }

        public override Type GetDataType()
        {
            return typeof(HyperCasualGameModelData);
        }
    }
}