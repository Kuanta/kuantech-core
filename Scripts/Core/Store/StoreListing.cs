using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [CreateAssetMenu(fileName = "StoreListing", menuName = "Kuantech/Store/StoreListing", order = 1)]
    public class StoreListing : ScriptableObject
    {
        public List<BuyableInfo> Buyables = new List<BuyableInfo>();
    }
}