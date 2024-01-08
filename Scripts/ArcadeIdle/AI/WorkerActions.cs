using System.Collections.Generic;
using System.Linq;
using Kuantech.AI;
using Kuantech.Utils;
using UnityEngine;
using YamlDotNet.Core.Tokens;

namespace Kuantech.ArcadeIdle
{
    public class NpcAction : BTLeafAction
    {
        protected ArcadeIdleNpc OwnerNpc;
        protected ResourceInventory OwnerInventory;

        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            OwnerNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            if(OwnerNpc == null)
            {
                Debug.LogError("Owner Npc is null");
            }
            OwnerInventory = OwnerNpc.GetModule<ResourceInventory>();
        }
    }
    
    public class StartAndWaitInteraction : NpcAction
    {
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            if(OwnerNpc.TargetInteractable == null)
            {
                ParentNode.CompleteNode(); //todo: This is not a failure for this node
                return;
            }
            if(!OwnerNpc.TargetInteractable.AddInteractor(OwnerNpc))
            {
                ParentNode.FailNode();
                return;
            }
            OwnerNpc.ToggleMovement(true);
            OwnerNpc.CalculateRemainingDistanceAndRotation();
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            base.Tick(ownerTree);
            bool result = OwnerNpc.IsInteracting();
            return result ? BTNode.NodeStatus.RUNNING : BTNode.NodeStatus.SUCCESS;
        } 
    }

    #region Registers And Getters
    /// <summary>
    /// Registers the current TargetActor of npc to the variable table
    /// </summary>
    public class RegisterTargetVenueActor : NpcAction
    {
        public string VariableKey;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            ownerTree.VariableTable.RegisterVariable(VariableKey, OwnerNpc.TargetVenueActor);
        }
    }

    public class SetTargetVenueActorFromTable : NpcAction
    {
        public string VariableKey;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            VenueActor targetActor = ownerTree.VariableTable.GetVariable<VenueActor>(VariableKey);
            if(targetActor == null)
            {
                ParentNode.FailNode();
            }
            OwnerNpc.TargetVenueActor = targetActor;
        }
    }

    /// <summary>
    /// Registers the currently set TargetInteractable to the variable table
    /// </summary>
    public class RegisterTargetInteractable : NpcAction
    {
        public string VariableKey;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            ownerTree.VariableTable.RegisterVariable(VariableKey, OwnerNpc.TargetInteractable);
        }
    }

    public class SetTargetInteractable : NpcAction
    {
        public string VariableKey;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            VenueInteractable targetInteractable = ownerTree.VariableTable.GetVariable<VenueInteractable>(VariableKey);
            if (targetInteractable == null)
            {
                ParentNode.FailNode();
            }
            OwnerNpc.TargetInteractable = targetInteractable;
        }
    }

    #endregion

    public class GatherFromInventory : NpcAction
    {
        public string ResourceId;
        private ResourceInventory _targetInventory;
        private ResourceData _resourceData;
        private bool _gatheredOnce = false;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _resourceData = ArcadeIdleManager.GetResourceData(ResourceId);
            _targetInventory = OwnerNpc.TargetInventory;
            _gatheredOnce = false;
        }
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if (_targetInventory == null || _resourceData == null)
            {
                return BTNode.NodeStatus.FAILURE;
            }

            //Hands of the worker is full
            if(!OwnerNpc.CharacterInventory.CanAcceptResource(_resourceData))
            {
                return BTNode.NodeStatus.SUCCESS;
            }

            //This inventory has no of the resource to give
            bool targetInventoryCanGive = _targetInventory.CanGiveResource(_resourceData);
            if (!targetInventoryCanGive && !_gatheredOnce)
            {
                return BTNode.NodeStatus.FAILURE;
            }else if(!targetInventoryCanGive && _gatheredOnce)
            {
                return BTNode.NodeStatus.SUCCESS;
            }

            if(ArcadeIdleActor.TransferResource(_targetInventory, OwnerNpc.CharacterInventory, _resourceData, true))
            {
                _gatheredOnce = true;
            }
            return BTNode.NodeStatus.RUNNING; 
        }

    }
    
    /// <summary>
    /// Tries to drop carried resources to target inventory
    /// </summary>
    public class DropToInventory : NpcAction
    {
        public string ResourceId;
        private ResourceInventory _targetInventory;
        private ResourceData _resourceData;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _resourceData = ArcadeIdleManager.GetResourceData(ResourceId);
            _targetInventory = OwnerNpc.TargetInventory;
        }
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if (_targetInventory == null || _resourceData == null)
            {
                return BTNode.NodeStatus.FAILURE;
            }

            //Worker gave all it can
            if (!OwnerNpc.CharacterInventory.CanGiveResource(_resourceData))
            {
                return BTNode.NodeStatus.SUCCESS;
            }

            //Target inventory can't accept anymore resources
            if (!_targetInventory.CanAcceptResource(_resourceData))
            {
                return BTNode.NodeStatus.FAILURE;
            }

            ArcadeIdleActor.TransferResource(OwnerNpc.CharacterInventory, _targetInventory, _resourceData, true);
            return BTNode.NodeStatus.RUNNING;
        }
    }

    /// <summary>
    /// This task, searches generators that produce a certain product
    /// </summary>
    public class SearchForGeneratorByProduct : NpcAction
    {
        public string ResourceId;
        public int MaxAmountOfCarry = 1000;
        private ResourceGenerator _targetGenerator;
        private ResourceData _resourceData;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _resourceData = ArcadeIdleManager.GetResourceData(ResourceId);
            List<ResourceGenerator> generators = OwnerNpc.CurrentVenue.GetResourceGeneratorsByProduct(_resourceData);
            if(generators == null || generators.Count == 0) return;

            //todo: Search for generator with highest amount of elements
            generators = generators.OrderByDescending(generator => 
                generator.OutputInventory.GetAvailableAmount(_resourceData.ResourceId)
            ).ToList();

            _targetGenerator = generators[0];
            OwnerNpc.TargetInventory = _targetGenerator.OutputInventory;
            OwnerNpc.TargetVenueActor = _targetGenerator;
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if(_targetGenerator == null) return BTNode.NodeStatus.FAILURE;
            return BTNode.NodeStatus.SUCCESS;
        }
    }

    public class SearchForGeneratorByInput : NpcAction
    {
        public string ResourceId;
        public int MaxAmountOfCarry = 1000;
        private ResourceGenerator _targetGenerator;
        private ResourceData _resourceData;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _resourceData = ArcadeIdleManager.GetResourceData(ResourceId);
            List<ResourceGenerator> generators = OwnerNpc.CurrentVenue.GetResourceGeneratorsByInput(_resourceData);
            if (generators == null || generators.Count == 0) return;

            generators = generators.OrderBy(generator =>
                    generator.InputInventory.GetAvailableAmount(_resourceData.ResourceId)
                ).ToList();

            //todo: Search for generator with highest amount of elements
            _targetGenerator = generators.GetRandomElement();
            OwnerNpc.TargetInventory = _targetGenerator.InputInventory;
            OwnerNpc.TargetVenueActor = _targetGenerator;
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if (_targetGenerator == null) return BTNode.NodeStatus.FAILURE;
            return BTNode.NodeStatus.SUCCESS;
        }
    }
    
    /// <summary>
    /// Searches for a generator to fill its input inventory
    /// </summary>
    public class SearchForGeneratorsToFill : NpcAction
    {
        public List<int> Tags;
        public string GeneratorKeyName;
        private ResourceGenerator _generatorToFill;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            List<ResourceGenerator> generators = OwnerNpc.CurrentVenue.GetVenueActors<ResourceGenerator>(Tags, Filter, InventoryCompare);
            if(generators.Count == 0) return;
            _generatorToFill = generators[0];
            OwnerNpc.TargetVenueActor = _generatorToFill;
            if(!GeneratorKeyName.IsNullOrEmpty())
            {
                ownerTree.VariableTable.RegisterVariable(GeneratorKeyName, _generatorToFill);
            }

            //Set the req list
            if (_generatorToFill == null)
            {
                ParentNode.FailNode();
                return;
            } 

            //Set the requirement list of the worker
            RequirementList reqList = OwnerNpc.GetModule<RequirementList>();
            if (reqList == null)
            {
                Debug.LogError($"{OwnerNpc.name} has no requirement list");
                ParentNode.FailNode();
                return;
            }

            reqList.EmptyList();
            List<ResourceIngredient> ingredients = _generatorToFill.GetCurrentRecipe().Ingredients;
            foreach (var ingredient in ingredients)
            {
                reqList.AddToShoppingList(ingredient.ResourceData, ingredient.RequiredAmount);
            }
        }

        /// <summary>
        /// Filter out generators that doesn't require an input or a dispenser provider.
        /// </summary>
        /// <param name="generator"></param>
        /// <returns></returns>
        private bool Filter(ResourceGenerator generator)
        {
            //Filter out generators that doesn't require any input ingredient
            bool requiresInput =  generator.InputInventory != null && !generator.GetCurrentRecipe().Ingredients.IsNullOrEmpty();
            if(!requiresInput) return false;

            //Check for available dispensers
            ArcadeIdleVenue venue = generator.ParentZone.ParentVenue;
            bool hasAvailableDispenser = false;
            foreach(var ingredient in generator.GetCurrentRecipe().Ingredients)
            {
                HashSet<ResourceDispenser> dispensers = venue.GetDispensersByResource(ingredient.ResourceData);
                if(dispensers.IsNullOrEmpty()) continue;
                foreach(var dispenser in dispensers)
                {
                    if(dispenser.CanDispenseResource(ingredient.ResourceData))
                    {
                        hasAvailableDispenser = true;
                        break;
                    }
                }
            }

            return hasAvailableDispenser && requiresInput;
        }

        private int InventoryCompare(ResourceGenerator a, ResourceGenerator b)
        {
            if(a.InputInventory == null || b.InputInventory == null) return 0;
            return a.InputInventory.GetCarriedResourcesCount().CompareTo(b.InputInventory.GetCarriedResourcesCount());
        }
    }

    /// <summary>
    /// Searches for a generator to fill its input inventory
    /// </summary>
    public class SearchForSinkersToFill : NpcAction
    {
        public List<int> Tags;
        public List<int> ZoneTags;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            ResourceSinker sinkerToFill;
            List<ResourceSinker> sinkers = OwnerNpc.CurrentVenue.GetVenueInteractables<ResourceSinker>(Tags,ZoneTags, Filter, InventoryCompare);
            if (sinkers.Count == 0) return;
            sinkerToFill = sinkers[0];
            OwnerNpc.TargetVenueActor = sinkerToFill.GetParentActor();
            OwnerNpc.TargetInteractable = sinkerToFill;

            //Set the req list
            if (sinkerToFill == null)
            {
                ParentNode.FailNode();
                return;
            }

            //Set the requirement list of the worker
            RequirementList reqList = OwnerNpc.GetModule<RequirementList>();
            if (reqList == null)
            {
                Debug.LogError($"{OwnerNpc.name} has no requirement list");
                ParentNode.FailNode();
                return;
            }

            reqList.EmptyList();
   
            foreach (var resource in sinkerToFill.AcceptedResources)
            {
                reqList.AddToShoppingList(resource, -1);
            }
        }

        /// <summary>
        /// Filter out sinkers that can't accept resources and doesn't have a valid accepted resources list
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        private bool Filter(ResourceSinker sinker)
        {
            if (!sinker.CanBeInteractedWith(OwnerNpc)) return false;
            //Filter out generators that doesn't require any input ingredient
            bool requiresInput = sinker.TargetInventory.CanAcceptResource(null) && sinker.AcceptedResources != null && sinker.AcceptedResources.Count > 0;
            if (!requiresInput) return false;

            //Check for available dispensers
            ArcadeIdleVenue venue = sinker.GetParentActor().ParentZone.ParentVenue;
            bool hasAvailableDispenser = false;
            foreach (var resource in sinker.AcceptedResources)
            {
                HashSet<ResourceDispenser> dispensers = venue.GetDispensersByResource(resource);
                if (dispensers.IsNullOrEmpty()) continue;
                foreach (var dispenser in dispensers)
                {
                    if (dispenser.CanDispenseResource(resource))
                    {
                        hasAvailableDispenser = true;
                        break;
                    }
                }
            }
            return hasAvailableDispenser;
        }

        private int InventoryCompare(ResourceSinker a, ResourceSinker b)
        {
            if (a.TargetInventory == null || b.TargetInventory == null) return 0;
            return a.TargetInventory.GetCarriedResourcesCount().CompareTo(b.TargetInventory.GetCarriedResourcesCount());
        }
    }

    /// <summary>
    /// Searches for sinkers that the npc can drop its current inventory.
    /// The action keeps running until owner npc empties its inventory
    /// </summary>
    public class SearchForSinkersToDropInventory : NpcAction
    {
        public List<int> Tags;
        public List<int> ZoneTags;
        private ResourceSinker _sinkerToDrop;

        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);

            //Check if Inventory is empty
            if (OwnerNpc.CharacterInventory.GetCarriedResourcesCount() == 0)
            {
                //All are emptied, return success
                ParentNode.CompleteNode();
                return;
            }

            SetSinkerToDropInventory();
            if(_sinkerToDrop == null)
            {
                ParentNode.FailNode();
                return;
            }
            _sinkerToDrop.AddInteractor(OwnerNpc);
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if(OwnerNpc.IsInteracting()) return BTNode.NodeStatus.RUNNING;

            if(OwnerNpc.CharacterInventory.GetCarriedResourcesCount() == 0)
            {
                //All are emptied, return success
                return BTNode.NodeStatus.SUCCESS;
            }
            
            SetSinkerToDropInventory();
            if(_sinkerToDrop == null)
            {
                //Couldn't find any sinker to drop inventory. Fail
                return BTNode.NodeStatus.FAILURE;
            }
            //We have a new sinker, keep on...
            _sinkerToDrop.AddInteractor(OwnerNpc);
            return BTNode.NodeStatus.RUNNING;
        }

        private void SetSinkerToDropInventory()
        {
            List<ResourceSinker> sinkers = OwnerNpc.CurrentVenue.GetVenueInteractables<ResourceSinker>(Tags, ZoneTags, Filter, InventoryCompare);
            if(sinkers.IsNullOrEmpty())
            {
                _sinkerToDrop = null;
                return;
            }
            _sinkerToDrop = sinkers[0];
            OwnerNpc.TargetInteractable = _sinkerToDrop;
            OwnerNpc.TargetVenueActor = _sinkerToDrop.GetParentActor();
        }
        /// <summary>
        /// Filter out sinkers that can't accept resources and doesn't have a valid accepted resources list
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        private bool Filter(ResourceSinker sinker)
        {
           if(!sinker.CanBeInteractedWith(OwnerNpc)) return false;
            foreach(var pair in OwnerNpc.CharacterInventory.HeldResources)
            {
                ResourceData data = ArcadeIdleManager.GetResourceData(pair.Key);
                if(pair.Value <= 0) continue; //Don't accept resources with 0 values
                if(!sinker.AcceptedResources.IsNullOrEmpty() && sinker.AcceptedResources.Contains(data)) return true; 
            }
            return false;
        }

        private int InventoryCompare(ResourceSinker a, ResourceSinker b)
        {
            if (a.TargetInventory == null || b.TargetInventory == null) return 0;
            return a.TargetInventory.GetCarriedResourcesCount().CompareTo(b.TargetInventory.GetCarriedResourcesCount());
        }
    }

    /// <summary>
    /// Searches a suitable dispenser using the Requirements list.
    /// </summary>
    public class SearchForDispenserAction : NpcAction
    {
        public List<int> DispenserTags;
        public List<int> ZoneTags;
        private ResourceDispenser _targetDispenser;
        private RequirementList _reqList;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _targetDispenser = null;
            _reqList = OwnerNpc.GetModule<RequirementList>();
            if(_reqList == null) return;
            if(_reqList.AreResourcesGathered())
            {
                ParentNode.CompleteNode();
                return;
            } 
            
            List<ResourceDispenser> candidDispensers = OwnerNpc.CurrentVenue.GetVenueInteractables<ResourceDispenser>(DispenserTags, ZoneTags, filter:DispenserFilter, comparer: Compare);
            if(candidDispensers.IsNullOrEmpty()) return;
            _targetDispenser = candidDispensers[0];
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree){
            if(_targetDispenser == null) return BTNode.NodeStatus.FAILURE;
            OwnerNpc.TargetInteractable = _targetDispenser;
            _targetDispenser.AddInteractor(OwnerNpc);
            return BTNode.NodeStatus.SUCCESS;
        }
        
        /// <summary>
        /// Filters out dispensers that can provide the resource
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        private bool DispenserFilter(ResourceDispenser dispenser)
        {
            if(dispenser == null) return false;
            if(!dispenser.CanBeInteractedWith(OwnerNpc)) return false;
            foreach(var req in _reqList.RequiredResources)
            {
                //Can dispenser provide the resource?
                if(dispenser.CanDispenseResource(req.Key)) return true;
            }
            return false;
        }

        private int Compare(ResourceDispenser actorA, ResourceDispenser actorB)
        {
            float distA = (OwnerNpc.transform.position - actorA.transform.position).sqrMagnitude;
            float distB = (OwnerNpc.transform.position - actorB.transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        }
    }

    
    public class RestInZoneAction : NpcAction
    {
        [KTTag("ZoneTags")]
        public List<int> ZoneTags;
        [KTTag("InteractabeTags")]
        public List<int> InteractableTags;

        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            if(OwnerNpc.GetCurrentZone() != null && ZoneTags.Contains(OwnerNpc.GetCurrentZone().ZoneTag))
            {
                //No need to assign
                ParentNode.CompleteNode();
                return;
            }
            OwnerNpc.EndInteraction(true); //Justin Case
            //Get a zone
            VenueZone zone = OwnerNpc.CurrentVenue.GetNearestZone(OwnerNpc, ZoneTags);
            if(zone == null)
            {
                ParentNode.FailNode();
                return ;
            }
            bool result = zone.AssignToRandomZoneInteractable(OwnerNpc, InteractableTags);
            if(!result)
            {
                ParentNode.FailNode();
            }
        }
    }

    public class EndInteractionAction : NpcAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            OwnerNpc.EndInteraction();
            return BTNode.NodeStatus.SUCCESS;
        }
    }

    public class ClearInventoryAction : NpcAction
    {
        public List<int> ResourceTags;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            OwnerNpc.CharacterInventory.ClearInventory(ResourceTags);
        }

    }
}