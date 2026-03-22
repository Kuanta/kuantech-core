using System;
using System.Collections.Generic;
using Kuantech.ArcadeIdle.UI;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    /// <summary>
    /// Represents the input requirements of a resource generation
    /// </summary>
    [Serializable]
    public struct ResourceIngredient
    {
        public ResourceData ResourceData;
        public int RequiredAmount;
    }

    [Serializable]
    public struct ResourceRecipe
    {
        public List<ResourceIngredient> Ingredients;
        public ResourceData ResourceToGenerate;
        public int AmountToGenerate;
    }
    
    public class ResourceGenerator : VenueActor
    {
        [Header("Properties")]
        public List<ResourceRecipe> Recipes;
        public ResourceInventory InputInventory;
        public ResourceInventory OutputInventory;
        public bool HaltGeneration = false;

        [Tooltip("Generation Period. If Greater than 0 second, the generator will periodically generate resource")]
        public float GenerationRate = 0.0f;

        [Tooltip("Amount of time required to generate a resource")]
        public float GenerationTime = 0.0f;
        private bool _generating = false;
        private float _generationStartTime;

        private float _lastGeneratedTime;

        [Header("Visuals")]
        [SerializeField] private Effect GenerationEffect;
        [SerializeField] private ProgressBar ProgressBar;

        private ResourceRecipe _currentRecipe;

        public override void Initialize(ActorSerializableData actorSerializableData = null)
        {
            base.Initialize(actorSerializableData);
            SetCurrentRecipe(0); //todo: This can be built upon for generators with multiple recipes
        }

        protected override void Update()
        {
            base.Update();
            if(HaltGeneration) return; //For debuggng
            if(_generating)
            {
                float elapsedTime = Time.time - _generationStartTime;
                if(ProgressBar != null) ProgressBar.SetFill(elapsedTime / GenerationTime);
                if(elapsedTime > GenerationTime)
                {
                    FinishGenerating();
                }
                return;
            }

            if(GenerationRate <= 0f) return;
            
            if (_currentRecipe.ResourceToGenerate == null || !OutputInventory.CanAcceptResource(_currentRecipe.ResourceToGenerate))
            {
                _lastGeneratedTime = Time.time;
                return;
            }

            if (Time.time - _lastGeneratedTime < GenerationRate || GenerationRate <= 0f) return;
            if(!CanGenerateResource(_currentRecipe.ResourceToGenerate)) return;
            GenerateResource();
        }

        /// <summary>
        /// Sets the current recipe
        /// </summary>
        public void SetCurrentRecipe(int recipeIndex)
        {
            _currentRecipe = Recipes[recipeIndex]; 
        }

        public ResourceRecipe GetCurrentRecipe()
        {
            return _currentRecipe;
        }

        /// <summary>
        /// Checks whether the generator can generate resource
        /// </summary>
        /// <returns></returns>
        public bool CanGenerateResource(ResourceData resource)
        {
            if(OutputInventory == null) return false;
            bool hasInventory =  OutputInventory.CanAcceptResource(resource);
            bool hasEnoughIngredients = HasEnoughIngredients();

            //todo: Check for input requirements
            return hasInventory && hasEnoughIngredients;
        }

        /// <summary>
        /// Checks whether this generator needs a given resource by looking up all recipes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RequiresResource(ResourceData data)
        {
            foreach(var recipe in Recipes)
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.ResourceData == data) return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Checks if the generator has enough input ingredients to complete the given recipe
        /// </summary>
        /// <returns></returns>
        public bool HasEnoughIngredients()
        {
            if(_currentRecipe.Ingredients == null || _currentRecipe.Ingredients.Count == 0) return true;
            if(InputInventory == null)
            {
                Debug.LogError($"Input Inventory for {name} is null");
                return false;
            }
            foreach(var ingredient in _currentRecipe.Ingredients)
            {
                int heldAmount = InputInventory.GetAvailableAmount(ingredient.ResourceData.Id);
                if(heldAmount < ingredient.RequiredAmount) return false;
            }
            return true;
        }

        /// <summary>
        /// Generates the resource
        /// </summary>
        public void GenerateResource()
        {
            StartGeneration();
        }

        /// <summary>
        /// Removes the required amount of ingredients
        /// </summary>
        private void SpendIngredients(ResourceRecipe recipe)
        {
            if (recipe.Ingredients == null || recipe.Ingredients.Count == 0) return;
            foreach (var ingredient in recipe.Ingredients)
            {
                //todo: For now, we can only return a single visual
                for(int i=0;i<ingredient.RequiredAmount;++i)
                {
                    ResourceVisual resourceVisual = InputInventory.RemoveResource(ingredient.ResourceData.Id, 1);
                    if (resourceVisual != null)
                    {
                        PoolManager.PoolObject(resourceVisual.gameObject);
                    }
                }
               
            }
        }

        public void StartGeneration()
        {
            _generating = true;
            _generationStartTime = Time.time;
            SpendIngredients(_currentRecipe);
            if(GenerationEffect != null) GenerationEffect.Play();
            if (ProgressBar != null) {
                ProgressBar.SetFill(0);
                ProgressBar.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Generates the 
        /// </summary>
        public void FinishGenerating()
        {
            _generating = false;
            _lastGeneratedTime = Time.time;
            for(int i=0;i<_currentRecipe.AmountToGenerate;++i)
            {
                //todo: Don't create more than inventory can handle
                if(!OutputInventory.CanAcceptResource(_currentRecipe.ResourceToGenerate)) break;
                OutputInventory.AddResource(_currentRecipe.ResourceToGenerate, null, false);
            }
            if (GenerationEffect != null) GenerationEffect.Stop();
            if(ProgressBar != null) ProgressBar.gameObject.SetActive(false);
        }

        public override void ResetActor()
        {
            _generating = false;
        }
    }
}