using System;
using System.Collections.Generic;
using Kuantech.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Kuantech.Core
{
    public class RagdollModule : ActorModule
    {
        [NonSerialized] public Ragdoll Ragdoll;
        [SerializeField] private List<GameObject> ObjectsToHideWhenRagdoll = new List<GameObject>();

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            ActorVisualHandler visualHandler = Actor.GetModule<ActorVisualHandler>();
            if (visualHandler != null)
            {
                visualHandler.OnActorVisualSet += OnActorVisualChanged;
                OnActorVisualChanged(visualHandler.CurrentActorVisual);
            }
        }
        
        public void FallIntoRagdoll()
        {
            if(Ragdoll == null) return;
            Ragdoll.ParentActor = Actor.transform;
            Ragdoll.EnableRagdoll();
            Actor.SetActorAnchor(Ragdoll.PelvisRigidbody.transform);
            ToggleObjectsVisibility(false);
        }

        public bool IsInRagdoll()
        {
            return Ragdoll.IsInRagdoll();
        }
        
        public void StandUpFromRagdoll()
        {
            if (Ragdoll == null) return;
            Ragdoll.GetUpFromRagdoll();
            Actor.SetActorAnchor(Actor.transform);
            ToggleObjectsVisibility(true);
        }
        
        private void ToggleObjectsVisibility(bool visible)
        {
            if (ObjectsToHideWhenRagdoll.IsNullOrEmpty()) return;
            foreach (var obj in ObjectsToHideWhenRagdoll)
            {
                if (obj != null)
                {
                    obj.SetActive(visible);
                }
            }
        }
        public void ApplyRagdollForce(Vector3 force)
        {
            FallIntoRagdoll();
            Ragdoll.PelvisRigidbody.AddForceAtPosition(force, Ragdoll.PelvisRigidbody.position, ForceMode.Impulse);
        }

        private void OnActorVisualChanged(ActorVisual visual)
        {
            if (visual == null) return;
            Ragdoll = visual.GetComponentInChildren<Ragdoll>();
        }

        public override void Reset()
        {
            if (Ragdoll != null)
            {
                Ragdoll.TurnoffRagdollState(); //Turnoff ragdoll state without any animation
            }
            Actor.SetActorAnchor(Actor.transform);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            Actor.SetActorAnchor(Actor.transform);
        }
    }
}