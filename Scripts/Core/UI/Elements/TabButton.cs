using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class TabButton : MonoBehaviour
    {
        public Button Button;
        [SerializeField] private GameObject OnImage;
        [SerializeField] private GameObject OffImage;
        [SerializeField] private UIElement ElementToOpen;
        [NonSerialized] public TabButtonsGroup ParentGroup;
        
        public void Initialize(TabButtonsGroup parentGroup)
        {
            ParentGroup = parentGroup;
            Button.onClick.AddListener(() =>
            {
                ParentGroup.OnChildButtonClicked(this);
            });
        }

        public void OpenTab()
        {
            OnImage.SetActive(true);
            OffImage.SetActive(false);
            ElementToOpen.Open();
        }

        public void CloseTab()
        {
            OnImage.SetActive(false);
            OffImage.SetActive(true);
            ElementToOpen.Close();
        }
    }
}