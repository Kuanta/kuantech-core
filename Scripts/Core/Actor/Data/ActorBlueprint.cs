using System;
using System.Collections.Generic;
using Kuantech.Core.HyperCasual;
using Kuantech.Midcore;
using UnityEngine;

namespace Kuantech.Core
{
    
    /// <summary>
    /// Defines a template for an actor.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorTemplate", menuName = "Kuantech/Midcore/Actor Template")]
    public class ActorBlueprint : MetadataAsset
    {
        [Header("Actor Prefab")] 
        public int FactionId;
        public Actor ActorPrefab;
        
        [Header("Blueprint Component")]
        [SerializeReference]
        public List<ActorBlueprintComponent> ActorBlueprintComponents = new List<ActorBlueprintComponent>();

        [Header("Visuals")]
        public ActorVisual ActorVisualPrefab;
        
        [Header("Progressable Data")]
        [Tooltip("Corresponding progressable data")]
        public ProgressableDataAsset ProgressableDataAsset;
        
        [Header("Price")] 
        public BuyableInfo BuyableInfo;
        
        private Dictionary<Type, ActorBlueprintComponent> _componentLookup;

        private void EnsureComponentLookupBuilt()
        {
            if (_componentLookup != null)
                return;

            _componentLookup = new Dictionary<Type, ActorBlueprintComponent>();
            foreach (var comp in ActorBlueprintComponents)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                if (!_componentLookup.ContainsKey(type))
                {
                    _componentLookup[type] = comp;
                }
            }
        }
        
        public T GetActorBlueprintComponent<T>() where T : ActorBlueprintComponent
        {
            EnsureComponentLookupBuilt();

            if (_componentLookup.TryGetValue(typeof(T), out var comp))
            {
                return comp as T;
            }

            return null;
        }
        
        public Actor CreateActor()
        {
            Actor actor = PoolManager.GetObjectFromPool(ActorPrefab.gameObject).GetComponent<Actor>();
            if (actor == null) return null;
            actor.Id = GetId();
            actor.FactionId = FactionId;

            if (ProgressableDataAsset != null)
            {
                //Set progressibles 
                ProgressionHandler progressionHandler = actor.GetComponentInChildren<ProgressionHandler>();
                if (progressionHandler != null) //Check if not null
                {
                    progressionHandler.ActorProgressableAsset = ProgressableDataAsset;
                }
            }

            actor.Initialize();
            if (actor.VisualHandler != null)
            {
                actor.VisualHandler.SetActorVisual(PoolManager.GetObjectFromPool(ActorVisualPrefab.gameObject).GetComponent<ActorVisual>());
            }
                
            //Sets blueprint comps
            ApplyComponentsToActor(actor);
            return actor;
        }

        public void ApplyComponentsToActor(Actor actor)
        {
            foreach(var blueprintComp in ActorBlueprintComponents)
            {
                blueprintComp.OnActorCreated(this, actor);
            }
        }
    }
}