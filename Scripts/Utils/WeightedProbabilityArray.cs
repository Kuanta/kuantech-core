using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class WeightedProbabilityArray<T>
    {
        [Serializable]
        public struct WPAElement
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

        public void NormalizeWeights()
        {
            float total = Elements.Sum(element => element.Probability);
            for (int i = 0; i < Elements.Count; ++i)
            {
                WPAElement element = Elements[i];
                element.Probability /= total;
                Elements[i] = element;
            }
            //totalWeight = 1;

        }
        public T Sample(float weightDecay=1)
        {
            if (Elements.Count == 0 )
            {
                throw new InvalidOperationException("WeightedProbabilityArray is empty.");
            }
            weightDecay = Mathf.Clamp(weightDecay, 0, 1);
            float totalWeight = 0f;
            for (int i = 0; i < Elements.Count; ++i)
            {
                totalWeight += Elements[i].Probability;
            }
            
            float randomValue = UnityEngine.Random.value * totalWeight;
            float sum = 0f;
            int elementIndex = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                sum += Elements[i].Probability;
                if (randomValue <= sum)
                {
                    elementIndex = i;
                    break;
                }
            }

            // If for some reason the random value exceeds the total weight,
            // return the last element as a fallback.
            WPAElement wpaElement = Elements[elementIndex];
            wpaElement.Probability *= weightDecay;
            NormalizeWeights();
            Elements[elementIndex] = wpaElement;
            return wpaElement.Element;
        }

        public void SetElementWeight(int elementIndex, float weight)
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

        public void Clear()
        {
            Elements.Clear();
        }
    }
}