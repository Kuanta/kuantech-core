using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class ActorVisualHandler : ActorModule
    {
        [Header("Slots")] 
        public Transform ActorVisualSlot;

        public bool ClearCurrentVisualOnDespawn = true;
        public ActorVisual CurrentActorVisual;
        
        //Events
        public UnityAction<ActorVisual> OnActorVisualRemoved;
        public UnityAction<ActorVisual> OnActorVisualSet;
        public UnityAction<ActorVisual> OnPostActorVisualSet; //A second post event for modules that need it. (for example slots must be set on first event trigger)

        public override void Initialize()
        {
            base.Initialize();
            if (CurrentActorVisual != null)
            {
                CurrentActorVisual.Initialize();
                CurrentActorVisual.OnAttachedToActor(Actor);
                CurrentActorVisual.ParentActor = Actor;
                OnActorVisualSet?.Invoke(CurrentActorVisual);
            }
        }

        public ActorVisual GetActorVisual()
        {
            return CurrentActorVisual;
        }
        
        /// <summary>
        /// Sets the current actor visual
        /// </summary>
        /// <param name="visual"></param>
        public void SetActorVisual(ActorVisual visual)
        {
            if(CurrentActorVisual == visual) return;
            ClearCurrentVisual();
            CurrentActorVisual = visual;
            CurrentActorVisual.gameObject.AttachToParent(ActorVisualSlot != null ? ActorVisualSlot : transform);
            CurrentActorVisual.OnAttachedToActor(Actor);
            CurrentActorVisual.ParentActor = Actor;
            OnActorVisualSet?.Invoke(CurrentActorVisual);
            OnPostActorVisualSet?.Invoke(CurrentActorVisual);
            visual.gameObject.SetActive(true);
        }

        public void ClearCurrentVisual()
        {
            if (CurrentActorVisual == null || !ClearCurrentVisualOnDespawn) return;
            CurrentActorVisual.OnRemovedFromActor(Actor);
            OnActorVisualRemoved?.Invoke(CurrentActorVisual);
            PoolManager.PoolObject(CurrentActorVisual.gameObject);
            CurrentActorVisual = null;
        }
        
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if(CurrentActorVisual == null) return;
            CurrentActorVisual.OnActorStateChanged(oldState, newState);
        }
    }
}