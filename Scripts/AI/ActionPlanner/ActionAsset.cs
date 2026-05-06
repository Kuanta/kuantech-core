using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.AI
{
    [CreateAssetMenu(menuName = "Kuantech/AI/ActionAsset")]
    public class ActionAsset : ScriptableObject
    {
        public string ActionId;
        [SerializeReference] public Action Template;

        public Action CreateAction(Actor owner)
        {
            if (Template == null) return null;
            Action action = Template.Clone();
            action.Owner = owner;
            return action;
        }
    }
}
