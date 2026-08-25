using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [Serializable]
    public class AnimationSetApplierItemComponentData : ItemComponentData
    {
        public RuntimeAnimatorController AnimSet;

        public override ItemComponent CreateInstance()
        {
            return new AnimationSetApplierItemComponent(AnimSet);
        }
    }
    public class AnimationSetApplierItemComponent : ItemComponent
    {
        public RuntimeAnimatorController AnimSet;
        public AnimationSetApplierItemComponent(RuntimeAnimatorController animSet)
        {
            AnimSet = animSet;    
        }
        public override void OnItemAdded(Item item)
        {
        }

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            Actor owner = GetOwner();
            if(owner == null) return;
            AnimationModule animationModule = owner.GetModule<AnimationModule>();
            if(animationModule == null) return;
            animationModule.ApplyAnimationSet(AnimSet);
        }

        public override void OnItemRemoved(Item item)
        {
        }

        public override void OnItemUnequipped(Item item)
        {
            Actor owner = GetOwner();
            if (owner == null) return;
            AnimationModule animationModule = owner.GetModule<AnimationModule>();
            if (animationModule == null) return;
            animationModule.ApplyDefaultAnimationSet();
        }

        public override void OnItemUsed(Item item)
        {
        }
    }
}