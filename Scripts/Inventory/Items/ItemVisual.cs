using System;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemVisual : MonoBehaviour
    {
        //Runtime
        [NonSerialized] public ItemData ItemData;
        [NonSerialized] public bool IsInPlace;

        public virtual void Spawn(ItemData parentItemData)
        {
            ItemData = parentItemData;
        }
        public void Despawn()
        {
            if (IsInPlace)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}