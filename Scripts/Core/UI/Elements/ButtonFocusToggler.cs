using System;
using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A little helper class to toggle the focus of a button. Simply enables canvas on the element.
    /// If a blocker tint is added on top of the ui, enabling the child canvas will bring it to front
    /// </summary>
    public class ButtonFocusToggler : MonoBehaviour
    {
        [SerializeField] private Canvas Canvas;
        [SerializeField] private GameObject ClickMeIndicator;
        
        
        bool _pendingHasValue;
        bool _pendingValue;

        private void Awake()
        {
            ToggleFocus(false);
        }

        public void ToggleFocus(bool toggle)
        {
            _pendingHasValue = true;
            _pendingValue = toggle;

            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            ApplyFocus(toggle);
        }
        
        public void SetSortingOrder(int sortingOrder)
        {
            if (Canvas == null) return;
            Canvas.sortingOrder = sortingOrder;
        }
        
        void OnEnable()
        {
            if (_pendingHasValue)
                ApplyFocus(_pendingValue);
        }

        void ApplyFocus(bool toggle)
        {
            if (ClickMeIndicator) ClickMeIndicator.SetActive(toggle);
            if (!Canvas) return;
            Canvas.overrideSorting = toggle;
            Canvas.sortingOrder    = toggle ? 1000 : 0;
        }
    }
}