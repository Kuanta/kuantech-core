using System;
using UnityEngine;

namespace Kuantech.Core
{
    public class RagdollModule : ActorModule
    {
        [NonSerialized] public Ragdoll Ragdoll;

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