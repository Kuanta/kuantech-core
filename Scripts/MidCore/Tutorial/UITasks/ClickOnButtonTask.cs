using Kuantech.Core;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Midcore.Tutorial
{
    public class ClickOnButtonTask : GameTask
    {
        [Header("Click On Button")]
        [SerializeField] private GameObject TintObject;
        [SerializeField] protected KtButton ButtonToClick;
        [SerializeField] protected ButtonFocusToggler FocusToggler;
        [SerializeField] protected int SortingOrder = 1000;
        
        [SerializeField] protected bool HideTintOnComplete = true;
        public override void StartTask()
        {
            base.StartTask();
            if(TintObject != null) TintObject.SetActive(true);
            if (ButtonToClick == null)
            {
                CompleteTask();
                return;
            }

            if (FocusToggler != null)
            {
                FocusToggler.ToggleFocus(true);
                FocusToggler.SetSortingOrder(SortingOrder);
            }
            ButtonToClick.OnPreButtonClicked -= OnButtonClicked;
            ButtonToClick.OnPreButtonClicked += OnButtonClicked;
        }

        public override void EndTask()
        {
            base.EndTask();
            if(FocusToggler != null) FocusToggler.ToggleFocus(false);
            if(TintObject != null && HideTintOnComplete) TintObject.SetActive(false);
        }

        private void OnButtonClicked(KtButton button)
        {
            CompleteTask();
        }
    }
}