using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.HyperCasual;
using UnityEngine;

namespace Kuantech.Merge
{
    public class MergeManager : SubManager
    {
        public Mergable MergablePrefab;
        public List<MergableTemplate> Mergables;
        private Dictionary<string, MergableTemplate> _idsToData;
 
        //Events
        public EventHandler<Mergable> OnMergeCombination; //Event when player combines 2 pawns and gets a succesfull combination
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _idsToData = new Dictionary<string, MergableTemplate>();
            foreach (MergableTemplate data in Mergables)
            {
                _idsToData[data.Id] = data;
            }  
        }

        
        #region Factory

        public MergableTemplate GetMergableData(string id)
        {
            return _idsToData[id];
        }
        public Mergable CreateMergable(string id, int level=1)
        {
            if (!_idsToData.ContainsKey(id)) return null;
            Mergable mergable = Instantiate(MergablePrefab).GetComponent<Mergable>();
            MergableTemplate data = GetMergableData(id);
            //Instantiate visual
            GameObject visual = Instantiate(data.Visual, mergable.transform, true);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            mergable.Initialize(data);
            mergable.SetLevel(level);
            return mergable;
        }
        
        public Mergable Merge(Mergable from, Mergable to, bool destroyExistings = true)
        {
            //Check the levels
            if (from.Level != to.Level || from == to)
            {
                return null;
            }
            
            //Check if they are the same thing
            if (to.MergableData.Id != from.MergableData.Id) return null;
            to.Upgrade();
            if (destroyExistings)
            {
                from.enabled = false;
                Destroy(from.gameObject);
            }
            return to;

        }

        public bool CanBeMerged(Mergable from, Mergable to)
        {
            if (from == to) return false; //Don't merge with itself
            if (from.Level != to.Level)
            {
                return false;
            }
            return from.MergableData.Id == to.MergableData.Id;
        }
        #endregion
    }
}