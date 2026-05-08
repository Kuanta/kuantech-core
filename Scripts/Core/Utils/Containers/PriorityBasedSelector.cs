using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.Utils;

namespace Kuantech.Utils
{
    public class PriorityBasedSelector<T> where T : IPriorityBasedSelectorElement
    {
        [Serializable]
        public class PriorityBasedSelectorElement
        {
            public T Element;
            public int Priority;
            public float Probability;
        }

        public List<PriorityBasedSelectorElement> Elements;

        private List<List<PriorityBasedSelectorElement>> _prioritySortedElements;
        
        public PriorityBasedSelector()
        {
            Elements = new List<PriorityBasedSelectorElement>();
            _prioritySortedElements = new List<List<PriorityBasedSelectorElement>>();
        }

        public void AddElement(PriorityBasedSelectorElement element, bool rebuild = true)
        {
            Elements.Add(element);
            if(rebuild) RebuildElements();
        }

        public void RemoveElement(T element, bool rebuild = true)
        {
            PriorityBasedSelectorElement elementToRemove = null;
            var cmp = EqualityComparer<T>.Default;
            foreach (var wpa in Elements)
            {
                if (cmp.Equals(wpa.Element, element))
                {
                    elementToRemove = wpa;
                    break;
                }
            }

            if (elementToRemove != null)
            {
                Elements.Remove(elementToRemove);
            }

            if(rebuild) RebuildElements();
        }   

        public void RebuildElements()
        {
            Dictionary<int, List<PriorityBasedSelectorElement>> priorityDict 
            = new Dictionary<int, List<PriorityBasedSelectorElement>>();

            foreach(var element in Elements)
            {
                if(!priorityDict.ContainsKey(element.Priority))
                {
                    priorityDict[element.Priority] = new List<PriorityBasedSelectorElement>();
                }
                priorityDict[element.Priority].Add(element);
            }

            List<int> keys = priorityDict.Keys.ToList();
            //Sort keys in descending fashion
            keys.Sort((a, b) => b - a); 
            for(int i=0;i< keys.Count();i++)
            {
                _prioritySortedElements.Add(new List<PriorityBasedSelectorElement>());
                foreach(var entry in priorityDict[keys[i]])
                {
                    _prioritySortedElements[i].Add(entry);
                }
            }
        }

        /// <summary>
        /// Samples a selected element
        /// </summary>
        /// <returns></returns>
        public T SelectElement(object userData = null)
        {
            T selectedEntry = default(T);
            if(_prioritySortedElements == null) return selectedEntry;
            //Travel action entries by priority
            foreach(var priorityElements in _prioritySortedElements)
            {
                WeightedProbabilityArray<PriorityBasedSelectorElement> eligibleEntries = new WeightedProbabilityArray<PriorityBasedSelectorElement>();

                foreach (var entry in priorityElements)
                {
                    if(entry.Element is IPriorityBasedSelectorElement ipbse && !ipbse.CanBeSelected(userData))
                    {
                        continue;
                    }
                    eligibleEntries.AddElement(entry, entry.Probability);
                }

                if(!eligibleEntries.IsNullOrEmpty())
                {
                    selectedEntry = eligibleEntries.Sample().Element;
                    break;
                }
            }

            return selectedEntry;
        }
    }
}