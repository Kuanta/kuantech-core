using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A manager to handle actor datas with pure serializable, networkable datas
    /// </summary>
    public class ActorDataManager : SubManager
    {
        [Serializable]
        public struct ActorPrefabData
        {
            public string ActorPrefabId;
            public Actor ActorPrefab;
        }

        [SerializeField] private List<ActorPrefabData> ActorPrefabDatas;
        [SerializeField] private Actor DefaultActorPrefab;
        [SerializeField] private List<ActorSpawnData> ActorSpawnData;
        [SerializeField] private List<ActorVisual> ActorVisuals;

        //Runtime
        private Dictionary<string,ActorSpawnData> _actorSpawnDatas;
        private Dictionary<string,ActorVisual> _actorVisuals;
        private Dictionary<string,Actor> _actorPrefabs;



        public async override UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);
            _actorSpawnDatas = new Dictionary<string, ActorSpawnData>();
            _actorVisuals    = new Dictionary<string, ActorVisual>();
            _actorPrefabs    = new Dictionary<string, Actor>();

            if(ActorSpawnData != null)
            {
                foreach (var actorSpawnData in ActorSpawnData)
                {
                    _actorSpawnDatas[actorSpawnData.ActorId] = actorSpawnData;
                }
            }

            if(ActorVisuals != null)
            {
                foreach (var actorVisual in ActorVisuals)
                {
                    _actorVisuals[actorVisual.ActorVisualId] = actorVisual;
                }
            }

            if(ActorPrefabDatas != null)
            {
                foreach (var actorPrefabData in ActorPrefabDatas)
                {
                    _actorPrefabs[actorPrefabData.ActorPrefabId] = actorPrefabData.ActorPrefab;
                }
            }
        }

        /// <summary>
        /// Get actor spawn data
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="actorSpawnData"></param>
        /// <returns></returns>
        public static bool GetActorSpawnData(string actorId, out ActorSpawnData actorSpawnData)
        {
            actorSpawnData = new ActorSpawnData();
            var ctx = GetContext<ActorDataManager>();
            if(ctx == null) return false;
            if(ctx._actorSpawnDatas == null) return false;
            if(!ctx._actorSpawnDatas.TryGetValue(actorId, out actorSpawnData)) return false;
            return true;
        }

        /// <summary>
        /// Get actor visual
        /// </summary>
        /// <param name="actorVisualId"></param>
        /// <returns></returns>
        public static ActorVisual GetActorVisual(string actorVisualId)
        {
            var ctx = GetContext<ActorDataManager>();
            if(ctx == null) return null;
            if (ctx._actorVisuals == null) return null;
            if(!ctx._actorVisuals.TryGetValue(actorVisualId, out var actorVisual))
            {
                return null;
            }
            return ctx._actorVisuals[actorVisualId];
        }

        public static Actor GetActorPrefab(string actorPrefabId=null)
        {
            var ctx = GetContext<ActorDataManager>();
            if(ctx == null) return null;
            if (ctx._actorPrefabs == null) return ctx.DefaultActorPrefab;
            if(!ctx._actorPrefabs.TryGetValue(actorPrefabId, out var actor))
            {
                return ctx.DefaultActorPrefab;
            }
            return ctx._actorPrefabs[actorPrefabId];
        }

        /// <summary>
        /// Instantiates the base actor prefab and applies ActorSpawnData (identity + faction).
        /// Does NOT call Initialize — caller decides whether to Initialize (standalone)
        /// or ServerManager.Spawn (networked). Module datas require Initialize first;
        /// call actor.ApplySpawnData(data) again after Initialize to load them.
        /// </summary>
        public static Actor InstantiateActor(string actorId)
        {

            Actor prefab = GetActorPrefab(actorId);
            if (prefab == null) return null;

            GameObject go = Instantiate(prefab.gameObject);
            if (!go.TryGetComponent<Actor>(out var actor)) { Destroy(go); return null; }
            actor.Initialize();
            if (GetActorSpawnData(actorId, out var spawnData)) 
            {
                actor.ApplySpawnData(spawnData);
            }
            return actor;
        }
    }
}

