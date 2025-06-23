using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class WeightedProbabilityArray<T>
    {
        [Serializable]
        public class WPAElement
        {
            public T Element;
            public float Probability;
        }

        public List<WPAElement> Elements;
        //private float totalWeight;

        public WeightedProbabilityArray()
        {
            Elements = new List<WPAElement>();
           // totalWeight = 0f;
        }

        public WeightedProbabilityArray(List<WPAElement> wpaElements)
        {
            Elements = new List<WPAElement>();
            //totalWeight = 0f;
            foreach (var wpa in wpaElements)
            {
                AddElement(wpa.Element, wpa.Probability);
            }
        }
        public void AddElement(T element, float weight)
        {
            if (weight == 0) return; //Don't add element with 0 prob
            Elements.Add(new WPAElement()
            {
                Element = element,
                Probability = weight,
            });
            //totalWeight += weight;
        }

        public float GetTotalWeight()
        {
            float totalWeight = 0f;

            // Calculate the new total weight after decay
            foreach (var element in Elements)
            {
                totalWeight += element.Probability;
            }

            return totalWeight;
        }
        public void NormalizeWeights()
        {
            float totalWeight = 0f;

            // Calculate the new total weight after decay
            foreach (var element in Elements)
            {
                totalWeight += element.Probability;
            }

            // Scale all weights so they sum back to a consistent total (e.g., 1)
            for (int i = 0; i < Elements.Count; i++)
            {
                WPAElement element = Elements[i];
                element.Probability /= totalWeight; // Normalize to make the sum equal 1
                Elements[i] = element;
            }

        }
        
        /// <summary>
        /// Sets all weights to 1
        /// </summary>
        public void UnifyWeights()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                WPAElement element = Elements[i];
                element.Probability = 1;
                Elements[i] = element;
            }
        }
        
        public T Sample(float weightDecay=1)
        {
            if (Elements.Count == 0)
            {
                throw new InvalidOperationException("WeightedProbabilityArray is empty.");
            }

            weightDecay = Mathf.Clamp(weightDecay, 0, 1);
            float totalWeight = 0f;

            // Calculate the total weight
            for (int i = 0; i < Elements.Count; ++i)
            {
                totalWeight += Elements[i].Probability;
            }

            // Randomly choose a point within the weight range
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float sum = 0f;
            int elementIndex = 0;

            // Find which element corresponds to the random value
            for (int i = 0; i < Elements.Count; i++)
            {
                sum += Elements[i].Probability;
                if (randomValue <= sum)
                {
                    elementIndex = i;
                    break;
                }
            }

            // Apply weight decay to the selected element
            WPAElement wpaElement = Elements[elementIndex];
            wpaElement.Probability *= weightDecay; // Decay the probability
            Elements[elementIndex] = wpaElement;
            
            // Enforce a minimum probability if needed to avoid zeroing out
            float minProbability = totalWeight * 0.01f; // Ensure at least 1% chance remains
            wpaElement.Probability = Mathf.Max(wpaElement.Probability, minProbability);

            Elements[elementIndex] = wpaElement;

            // Renormalize the weights to maintain a balanced probability distribution
            NormalizeWeights();

            return wpaElement.Element;
        }

        public void SetElementWeightByIndex(int elementIndex, float weight)
        {
            float newTotal = weight;
            for (int i = 0; i < Elements.Count; ++i)
            {
                WPAElement existing = Elements[i];
                if (i == elementIndex)
                {
                    existing.Probability = weight;
                    Elements[i] = existing;
                }
                else
                {
                    newTotal += existing.Probability;
                }
            }
        }

        public void SetElementWeight(T element, float weight)
        {
            for (int i = 0; i < Elements.Count; ++i)
            {
                WPAElement existing = Elements[i];
                if (existing.Element.Equals(element))
                {
                    existing.Probability = weight;
                    Elements[i] = existing;
                }
            }
        }

        public void DecayElementWeight(T element, float weight)
        {
            for (int i = 0; i < Elements.Count; ++i)
            {
                WPAElement existing = Elements[i];
                if (existing.Element.Equals(element))
                {
                    existing.Probability = weight * existing.Probability; //Decay the prob
                    Elements[i] = existing;
                }
            }
        }
        
        public float GetElementWeight(T element)
        {
            for (int i = 0; i < Elements.Count; ++i)
            {
                WPAElement existing = Elements[i];
                if (existing.Element.Equals(element))
                {
                    return existing.Probability;
                }
            } 
            return 0;
        }
        public void Clear()
        {
            Elements.Clear();
        }
    }
}