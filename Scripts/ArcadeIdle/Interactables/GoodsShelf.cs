using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    
    /// <summary>
    /// Goods Shelf is an interactable where npcs come with a shopping list. 
    /// </summary>
    public class GoodsShelf : VenueInteractable
    {
        [Header("Vendor")]
        [Tooltip("If goods shelf requires vendor, set this to true. Without a vendor, transaction won't happen.")]
        [SerializeField] private bool RequiresVendor;
        [SerializeField] private InteractionSlot VendorSlot; 
        [SerializeField] private ArcadeIdleNpc VendorPrefab;
        private ArcadeIdleNpc _currentVendor = null;

        [Header("Inventories")]
        public ResourceInventory InputInventory; //Inventory that holds the goods
        public ResourceInventory OutputInventory; //Inventory that holds the output. Probably the generated money
        public ResourceData OutputResource;
        
        [Header("Template Shopping List")]
        public List<RequirementListTemplate> TemplateRequirementLists;

        public bool GenerateResource; //If set to true, Output resource will be generated
        [NonSerialized] public List<RequirementListEntry> ChangedEntries = new List<RequirementListEntry>();

        public bool ShowRequirementListIndicator = false;

        protected override void HandleActor(ArcadeIdleCharacter character)
        {
            //Check if vendor requiring goods shelves have vendors
            if(!VendorSlot.HasInteractor() && RequiresVendor) return;

            //Don't handle the vendor
            if(VendorSlot.IsCharacterInTheSlot(character)) return;

            RequirementList shoppingList = character.GetModule<RequirementList>();

            //Check npcs shopping list
            if (shoppingList == null)
            {
                Debug.LogWarning("Npc has no shopping list");
                character.EndInteraction();
            }

            bool ordersSatisfied = true;
            ChangedEntries.Clear();
            
            //Are orders satisfied?
            foreach (var pair in shoppingList.RequiredResources)
            {
                RequirementListEntry entry = pair.Value;

                //Check if gathered
                bool gathered = shoppingList.IsResourcesGathered(pair.Key);
                if (gathered) continue;

                ordersSatisfied = false;
                string resourceId = entry.ResourceData.ResourceId; 
                int requiredAmount = entry.RequiredAmount - entry.GatheredAmount; //How many resources left?
                int availableAmount = InputInventory.GetHeldAmount(resourceId); //How many resources does the shelf have?
                int amountToGive = Mathf.Min(requiredAmount, availableAmount);

                ResourceInventory characterInventory = character.GetModule<ResourceInventory>();
                bool resourceGiven = false;
                for (int i = 0; i <  amountToGive; ++i)
                {
                    resourceGiven |= ArcadeIdleActor.TransferResource(InputInventory, characterInventory, entry.ResourceData,true);
                }
                if (resourceGiven)
                {
                    int gatheredAmount = entry.RequiredAmount - characterInventory.GetHeldAmount(entry.ResourceData.ResourceId);
                    shoppingList.Indicator.UpdateResourceAmount(entry.ResourceData, gatheredAmount);
                }

                //Check if gathered
                if (shoppingList.IsResourcesGathered(pair.Key))
                {
                    entry.PaidFor = true;
                }
                ChangedEntries.Add(entry);
                if (!GenerateResource) continue;
                for (int i = 0; i < amountToGive; ++i)
                {
                    OutputInventory.AddResource(OutputResource, null, false);
                }
            }

            foreach (var entry in ChangedEntries)
            {
                shoppingList.RequiredResources[entry.ResourceData] = entry;
            }
            if(ordersSatisfied)
            {
                character.EndInteraction();
            }
        }

        public override void OnActorReachedInteractable(ArcadeIdleCharacter character)
        {
            base.OnActorReachedInteractable(character);
            if(VendorSlot.IsCharacterInTheSlot(character)) return;
            RequirementList reqList = character.GetModule<RequirementList>();
            if(reqList == null) 
            {
                return;
            }
            if((reqList.RequiredResources == null || reqList.RequiredResources.Count == 0) && !TemplateRequirementLists.IsNullOrEmpty())
            {
                List<RequirementListEntry> template = TemplateRequirementLists[UnityEngine.Random.Range(0, TemplateRequirementLists.Count)].Requirements;
                foreach (var req in template)
                {
                    reqList.AddToShoppingList(req.ResourceData, req.RequiredAmount);
                }
            }
            reqList.InitializeIndicator();
            if(ShowRequirementListIndicator) reqList.ToggleIndicator(true);
        }

        public override void OnActorRemoved(ArcadeIdleCharacter character)
        {
            base.OnActorRemoved(character);
            if (VendorSlot.IsCharacterInTheSlot(character)) return;
            RequirementList reqList = character.GetModule<RequirementList>();
            if(reqList != null) reqList.ToggleIndicator(false);
        }
        public void SpawnVendor()
        {
            if(_currentVendor != null) return; //A vendor already spawned
            _currentVendor = Instantiate(VendorPrefab);
            _currentVendor.Spawn(GetParentZone().ParentVenue, null);
            AssignActorToSlot(_currentVendor, VendorSlot);
            //Warp to slot
            _currentVendor.SetDestination(new WorldPoint(){
                Target = VendorSlot.transform,
            });
            _currentVendor.WarpToTarget();
        }
    }
}