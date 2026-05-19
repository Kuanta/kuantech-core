using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct DependencyData
    {
        public AttributeAsset DependentAttribute;
        [SerializeReference] public KtFormula DependencyFormula;
    }

    [CreateAssetMenu(fileName = "AttributeAsset", menuName = "Kuantech/Rpg/StatAttribute")]
    public class AttributeAsset : MetadataAsset {
        public List<DependencyData> Dependencies;
    }
}