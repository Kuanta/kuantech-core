using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class GeneratorInteractable : InteractableComponent
    {
        [SerializeField] private ResourceGenerator ResourceGenerator;
        [SerializeField] private float GenerationTime;
        [SerializeField] private float SpeedMultiplierPerInteractor = 0.1f;

        private float _timer = 0f;

        public override void UpdateComponent()
        {
            int numberOfInteractors = ParentInteractable.GetNumberOfInteractingCharacters();
            if (numberOfInteractors == 0)
            {
                _timer = 0;
                return;
            }
           
           _timer += Time.deltaTime * (1 + (numberOfInteractors) * SpeedMultiplierPerInteractor);
           if(_timer >= GenerationTime)
           {
                //Check if resource generator can generate resource
                if(ResourceGenerator.GetCurrentRecipe().ResourceToGenerate == null)
                {
                    Debug.LogError("Ananın amı");
                }
                if (!ResourceGenerator.CanGenerateResource(ResourceGenerator.GetCurrentRecipe().ResourceToGenerate))
                {
                    _timer = GenerationTime;
                    return;
                }
                ResourceGenerator.GenerateResource();
                _timer = 0f;
            }
        }

    }
}