using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class UnitPurchaseButton : MonoBehaviour
    {
        [Header("Unit To Spawn")] 
        [SerializeField] private ActorTemplateAsset UnitToSpawn;
        
        [Header("Components")] 
        public Draggable Draggable;

        [Header("Price Tag")] 
        [SerializeField] private PriceTag PriceTag;

        private void Awake()
        {
            if (Draggable == null) return;
            Draggable.OnSuccesfullDropEvent += OnDrop;
            
        }

        private void OnDrop(DropInformation dropInfo)
        {
            
        }
    }
}