using System;
using System.Collections.Generic;
using Kuantech.Core.Database;
using UnityEngine;

namespace Kuantech.Core.Data
{
    [CreateAssetMenu(fileName = "DataCollection", menuName = "Kuantech/Data/DataCollection")]
    public class DataCollection : ScriptableObject
    {
        [Serializable]
        public struct DataColllectionEntry
        {
            public string Id;
            [SerializeReference]
            public KtDataType Data;
        }
        public List<DataColllectionEntry> Entries = new List<DataColllectionEntry>();
    }
}