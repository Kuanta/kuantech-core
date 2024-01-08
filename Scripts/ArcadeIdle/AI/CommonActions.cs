using System.Collections.Generic;
using Kuantech.AI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ReturnSuccessAction : BTLeafAction
    {
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            ParentNode.CompleteNode();
        }
    }
    public class GoToTargetAction : BTLeafAction
    {
        private ArcadeIdleNpc ownerNpc;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            ownerNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            ownerNpc.ToggleMovement(true);
            ownerNpc.CalculateRemainingDistanceAndRotation();
        }
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if (ownerNpc.ReachedDestination())
            {
                ownerNpc.ToggleMovement(false);
                return BTNode.NodeStatus.SUCCESS;
            }
            return BTNode.NodeStatus.RUNNING;
        }
    }
    
    public class SearchVenueActorByTag : BTLeafAction
    {
        public string Tag;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
        }
    }

    public class SearchInteractableAction : NpcAction
    {
        [KTTag("VenueTags")]
        public List<int> InteractableTags;
        public List<int> ZoneTags;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            List<VenueInteractable> interactables = OwnerNpc.CurrentVenue.GetVenueInteractables<VenueInteractable>(InteractableTags, ZoneTags, FilterInteractable, CompareInteractables);
            if(interactables == null || interactables.Count == 0)
            {
                ParentNode.FailNode();
                return;
            }
            OwnerNpc.TargetInteractable = interactables[0];
            bool result = interactables[0].AddInteractor(OwnerNpc);
            if(!result)
            {
                Debug.LogError("We have an issue in node:"+ParentNode.Name);
                ParentNode.FailNode();
            }
        }

        private int CompareInteractables(VenueInteractable a, VenueInteractable b)
        {
            //Compare the slot availability
            bool ahasSlot = a.HasAvailableSlots(OwnerNpc);
            bool bHasSlot = b.HasAvailableSlots(OwnerNpc);
            if(ahasSlot && !bHasSlot) return -1;
            else if(!ahasSlot && bHasSlot) return 1;

            if(!ahasSlot && !bHasSlot)
            {
                //Compare queue count
                int queueCountA = a.GetActorCountInQueue(OwnerNpc);
                int queueCountB = b.GetActorCountInQueue(OwnerNpc);
                if(queueCountA != queueCountB)
                {
                    return queueCountA.CompareTo(queueCountB);
                }
            }
            float distA = Vector3.SqrMagnitude(a.transform.position - OwnerNpc.transform.position);
            float distB = Vector3.SqrMagnitude(b.transform.position - OwnerNpc.transform.position);
            return distA.CompareTo(distB);
        }
        private bool FilterInteractable(VenueInteractable venueInteractable)
        {
            return venueInteractable.CanBeInteractedWith(OwnerNpc);
        }
    }
    
    public class GoToNearestZoneAction : BTLeafAction
    {
        [KTTag("ZoneTags")]
        public List<int> ZoneTags;
        [KTTag("InteractableTags")]
        public List<int> InteractableTags;
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            ArcadeIdleNpc arcadeIdleNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            if (arcadeIdleNpc.CurrentVenue == null) return BTNode.NodeStatus.FAILURE;
            
            VenueZone zone = arcadeIdleNpc.CurrentVenue.GetNearestZone(arcadeIdleNpc, ZoneTags);
            if(zone == null)
            {
                return BTNode.NodeStatus.FAILURE;
            }
            zone.AssignToRandomZoneInteractable(arcadeIdleNpc, InteractableTags);
            return BTNode.NodeStatus.SUCCESS;
        }
    }

    /// <summary>
    /// Checks if npc is in a zone with a desired tag. If the npc is already in a such zone, returns success
    /// </summary>
    public class CheckZoneAction : BTLeafAction
    {
        [KTTag("ZoneTags")]
        public List<int> ZoneTags;

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            ArcadeIdleNpc arcadeIdleNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            if(arcadeIdleNpc.AssignedInteractable == null) return BTNode.NodeStatus.FAILURE;
            VenueZone currZone = arcadeIdleNpc.AssignedInteractable.GetParentZone();
            if(currZone == null) return BTNode.NodeStatus.FAILURE;
            if(ZoneTags.Contains(currZone.ZoneTag)) return BTNode.NodeStatus.SUCCESS;
            return BTNode.NodeStatus.FAILURE;
        }
    }
    

    public class WaitForInteractionEndAction : BTLeafAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            ArcadeIdleNpc arcadeIdleNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            bool result = arcadeIdleNpc.IsInteracting();
            return result ? BTNode.NodeStatus.RUNNING : BTNode.NodeStatus.SUCCESS;
        }
    }

    public class SearchForExit : BTLeafAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            ArcadeIdleNpc arcadeIdleNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            if ( arcadeIdleNpc.CurrentVenue == null) return BTNode.NodeStatus.SUCCESS;
            arcadeIdleNpc.SetDestination(arcadeIdleNpc.CurrentVenue.GetRandomExit().SampleWorldPoint());
            return BTNode.NodeStatus.SUCCESS;
        }
    }

    public class DespawnActorAction : BTLeafAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            ArcadeIdleNpc arcadeIdleNpc = ownerTree.VariableTable.GetVariable<ArcadeIdleNpc>("Owner");
            arcadeIdleNpc.Despawn();
            return BTNode.NodeStatus.SUCCESS;
        }
    }

    public class WaitAction : BTLeafAction
    {
        public float WaitTime;
        private float _startTime;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _startTime = Time.time;
        }

        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if(Time.time - _startTime > WaitTime) return BTNode.NodeStatus.SUCCESS;
            return BTNode.NodeStatus.RUNNING;
        }
    }

}