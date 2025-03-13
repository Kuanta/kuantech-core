using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Data;
using UnityEngine;

namespace Kuantech.Rpg
{
    [Serializable]
    public struct BaseBody
    {
        public Enums.Races Race { get; set; }
        public Enums.Genders Gender { get; set; }
        public GameObject Body;
    }

    public class BaseBodyComparer : IEqualityComparer<BaseBody>
    {
        public bool Equals(BaseBody x, BaseBody y)
        {
            return x.Race == y.Race && x.Gender == y.Gender;
        }

        public int GetHashCode(BaseBody obj)
        {
            return $"{obj.Race.ToString()}+{obj.Gender.ToString()}".GetHashCode();
        }
    }
    [Serializable]
    public struct Visuals
    {
        public List<GameObject> HairModels;
        public List<GameObject> EyebrowModels;
        public List<GameObject> BeardModels;
    }
    [Serializable]
    public struct GenderVisuals
    {
        public Enums.Genders Gender;
        public Visuals Visuals;
    }
    [Serializable]
    public struct RaceVisuals
    {
        public Enums.Races Race;
        public List<GenderVisuals> GenderVisuals;
        public Dictionary<Enums.Genders, GenderVisuals> GenderVisualsMap;
    }
    
    
    public class VisualizationManager : Singleton<VisualizationManager>
    {
        [Header("Customizables")]
        [SerializeField] public List<RaceVisuals> RaceVisualsList;
        [SerializeField] public List<BaseBody> BaseBodies;

        [Header("Premades")] 
        public List<GameObject> Premades;
        
        private Dictionary<Enums.Races, RaceVisuals> RaceVisualsMap;
        private Dictionary<BaseBody, GameObject> BaseBodiesMap;
        
        public void Awake()
        {
            BaseBodiesMap = new Dictionary<BaseBody, GameObject>(new BaseBodyComparer());
            RaceVisualsMap = new Dictionary<Enums.Races, RaceVisuals>();
            for (int i = 0; i < RaceVisualsList.Count; i++)
            {
                RaceVisuals raceVisual = RaceVisualsList[i];
                raceVisual.GenderVisualsMap = new Dictionary<Enums.Genders, GenderVisuals>();
                for (int j = 0; j < raceVisual.GenderVisuals.Count; j++)
                {
                    GenderVisuals genderVisual = raceVisual.GenderVisuals[i];
                    raceVisual.GenderVisualsMap[genderVisual.Gender] = genderVisual;
                }
                RaceVisualsMap[RaceVisualsList[i].Race] = raceVisual;
            }

            for (int i = 0; i < BaseBodies.Count; i++)
            {
                BaseBodiesMap[BaseBodies[i]] = BaseBodies[i].Body;
            }
        }

        public GameObject GetBaseBody(Enums.Races race, Enums.Genders gender)
        {
            return BaseBodiesMap[new BaseBody {Race = race, Gender = gender}];
        }
        
        /// <summary>
        /// Returns all the available visualizations for a given gender and race
        /// </summary>
        /// <param name="race"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public Visuals GetVisuals(Enums.Races race, Enums.Genders gender)
        {
            return (RaceVisualsMap[race].GenderVisualsMap)[gender].Visuals;
        }

        public List<GameObject> GetVisualFields(Enums.Races race, Enums.Genders gender, Enums.VisualFields field)
        {
            Visuals visuals = GetVisuals(race, gender);
            switch (field)
            {
                case Enums.VisualFields.HairType:
                    return visuals.HairModels;
                case Enums.VisualFields.EyebrowType:
                    return visuals.EyebrowModels;
                case Enums.VisualFields.BeardType:
                    return visuals.BeardModels;
                default:
                    return new List<GameObject>(); // Return empty list
            }
        }

        public GameObject GetVisualObject(Enums.Races race, Enums.Genders gender, Enums.VisualFields field, int id)
        {
            List<GameObject> availableObjects = GetVisualFields(race, gender, field);
            if (availableObjects.Count == 0) return null;
            id %= availableObjects.Count; //Keep it in the range
            return availableObjects[id];
        }

        public GameObject GetPremade(int index)
        {
            if (Premades == null || Premades.Count <= index || index < 0) return null;
            return PoolManager.GetObjectFromPool(Premades[index]);
        }
    }
  
}