using System;
using System.Collections.Generic;
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
    public class AttributeAsset : ScriptableObject {
        public string Id;
        public string Name;
        public Sprite Icon;
        public string Description;
        public List<DependencyData> Dependencies;
    }
}