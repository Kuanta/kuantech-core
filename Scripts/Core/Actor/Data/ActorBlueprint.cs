using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using UnityEngine;

namespace Kuantech.Midcore
{
    
    /// <summary>
    /// Defines a template for an actor.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorTemplate", menuName = "Kuantech/Midcore/Actor Template")]
    public class ActorBlueprint : MetadataAsset
    {
        [Header("Actor Prefab")] 
        public Actor ActorPrefab;
        
        [Header("Blueprint Component")]
        [SerializeReference]
        public List<ActorBlueprintComponent> ActorBlueprintComponents = new List<ActorBlueprintComponent>();
        
        // [Header("Stats")]
        // public List<AttributeDefinition> Attributes;

        [Header("Visuals")]
        public ActorVisual ActorVisualPrefab;
        
        [Header("Progressable Data")]
        [Tooltip("Corresponding progressable data")]
        public ProgressableDataAsset ProgressableDataAsset;
        
        [Header("Price")] 
        public BuyableInfo BuyableInfo; //todo: Remove this from here
        
        public Actor CreateActor()
        {
            Actor actor = PoolManager.GetObjectFromPool(ActorPrefab.gameObject).GetComponent<Actor>();
            if (actor == null) return null;
            actor.Id = GetId();
            

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
            foreach(var blueprintComp in ActorBlueprintComponents)
            {
                blueprintComp.OnActorCreated(actor);
            }
            return actor;
        }
    }
}