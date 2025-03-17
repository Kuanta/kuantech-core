using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class ActorVisualHandler : ActorModule
    {
        [Header("Slots")] 
        public Transform ActorVisualSlot;
        
        public ActorVisual CurrentActorVisual;
        
        //Events
        public UnityAction<ActorVisual> OnActorVisualRemoved;
        public UnityAction<ActorVisual> OnActorVisualSet;

        public override void Initialize()
        {
            base.Initialize();
            if (CurrentActorVisual != null)
            {
                CurrentActorVisual.Initialize();
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
            ClearCurrentVisual();
            CurrentActorVisual = visual;
            CurrentActorVisual.gameObject.AttachToParent(ActorVisualSlot != null ? ActorVisualSlot : transform);
            CurrentActorVisual.Initialize();
            OnActorVisualSet?.Invoke(CurrentActorVisual);
            
        }

        public void ClearCurrentVisual()
        {
            if (CurrentActorVisual == null) return;
            OnActorVisualRemoved?.Invoke(CurrentActorVisual);
            Destroy(CurrentActorVisual.gameObject);
        }
    }
}