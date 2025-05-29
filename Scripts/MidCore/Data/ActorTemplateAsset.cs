using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    
    /// <summary>
    /// Defines a template for an actor.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorTemplate", menuName = "Kuantech/Midcore/Actor Template")]
    public class ActorTemplateAsset : MetadataAsset
    {
        [Header("Actor Prefab")] 
        public Actor ActorPrefab;
        
        [Header("Stats")]
        public List<AttributeDefinition> Attributes;

        [Header("Visuals")]
        public ActorVisual ActorVisualPrefab;
        
        [Header("Progressable Data")]
        public ProgressableDataAsset ProgressableDataAsset;
        
        [Header("Price")] 
        public BuyableInfo BuyableInfo;
        
        public Actor CreateActor()
        {
            Actor actor = PoolManager.GetObjectFromPool(ActorPrefab.gameObject).GetComponent<Actor>();
            if (actor == null) return null;

            if (!Attributes.IsNullOrEmpty())
            {
                //Apply stat defaults before initialize for upgrades to take place
                StatsModule statsModule = actor.GetComponentInChildren<StatsModule>();
                if (statsModule != null)
                {
                    statsModule.Stats = Attributes;
                }
            }

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
            return actor;
        }
    }
}