using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class RequirementListIndicator : MonoBehaviour {
        [SerializeField] private Transform ElementsParent;
        [SerializeField] private ResourceIndicatorElement Prefab;

        [Header("Content sizing")]
        [SerializeField] private RectTransform RectTransform;
        [SerializeField] private VerticalLayoutGroup VerticalLayoutGroup;
        [SerializeField] private float Padding = 0f;
        [SerializeField] private float ChildSize = 100.0f;
        [SerializeField] private float ChildSpacing = 5.0f;
        
        private Dictionary<ResourceData, ResourceIndicatorElement> _elements;
        
        /// <summary>
        /// Sets the requirement list 
        /// </summary>
        public void Setup(RequirementList list)
        {
            ElementsParent.DestroyAllChildren();
            _elements = new Dictionary<ResourceData, ResourceIndicatorElement>();
            foreach(var req in list.RequiredResources)
            {
                if(req.Value.RequiredAmount <= 0) continue;
                ResourceIndicatorElement element = Instantiate(Prefab);
                element.SetResource(req.Key);
                element.SetAmount(req.Value.RequiredAmount);
                element.transform.SetParent(ElementsParent, false);
                _elements[req.Key] = element;
            }
            UpdateHeight();
        }
        
        /// <summary>
        /// Updates the amount text
        /// </summary>
        /// <param name="data"></param>
        /// <param name="newAmount"></param>
        public void UpdateResourceAmount(ResourceData data, int newAmount)
        {
            _elements[data].SetAmount(newAmount);
            _elements[data].gameObject.SetActive(newAmount > 0);
            UpdateHeight();
        }

        private void UpdateHeight()
        {
            int resourceCountToShow = 0;
            for(int i=0;i<ElementsParent.childCount;++i)
            {
                if(ElementsParent.transform.GetChild(i).gameObject.activeSelf) resourceCountToShow++;
            }
            if(resourceCountToShow <= 0)
            {
                gameObject.SetActive(false);
            }
            Rect rect = RectTransform.rect;
            float height = resourceCountToShow * ChildSize + 2 * Padding + ChildSpacing * (Mathf.Max(resourceCountToShow - 1, 0));
            rect.height = height;
            RectTransform.sizeDelta = new Vector2(rect.width, height);
        }
    }
}