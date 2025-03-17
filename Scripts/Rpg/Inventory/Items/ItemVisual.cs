using System;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    public class ItemVisual : MonoBehaviour
    {
        //Runtime
        [NonSerialized] public bool IsInPlace;
        
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