using System;
using Kuantech.Core;
using Kuantech.Rpg;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Kuantech.Midcore
{
    [Serializable]
    public class ProgressibleData
    {
        public string ParentProgressibleId; //Used for sub upgrades
        public string Id;//Id of the progresable
        [SaveableField] public LevelVariable Rank;//Rank of the progressable

        public ProgressibleData(ProgressableDataAsset progressableDataAsset = null)
        {
            if (progressableDataAsset != null)
            {
                SetFromAsset(progressableDataAsset);
            }
            else
            {
                Id = string.Empty;
                Rank = null;
            }
        }
   
        public void SetFromAsset(ProgressableDataAsset progressableDataAsset)
        {
            if (progressableDataAsset == null)
            {
                Debug.LogError("Null progressable data asset");
                return;
            }
            Id = progressableDataAsset.Id;
            Rank = new LevelVariable(progressableDataAsset.LevelVariableData);
        }
        
        public void SetRank(int rank)
        {
            Rank.SetLevel(rank);
        }

        public void AddExperience(float experience)
        {
            Rank.AddValue(experience);
        }
        
        public void SetExperience(float experience)
        {
            Rank.SetValue(experience);
        }
        
        public int GetRankValue()
        {
            if (Rank == null) return 0;
            return Rank.CurrentLevel;
        }

        public LevelVariable GetRank()
        {
            return Rank;
        }
    }
}