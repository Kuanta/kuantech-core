using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class TabGroupButton : MonoBehaviour {
        public int ButtonIndex;
        [SerializeField] private Button Button;
        [SerializeField] private GameObject Toggled;
        [SerializeField] private GameObject Disabled;
        public EventHandler<int> OnButtonPressed;

        private void Start()
        {
            Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            OnButtonPressed?.Invoke(this, ButtonIndex);
        }

        public void Toggle(bool toggle)
        {
            Toggled.SetActive(toggle);
            Disabled.SetActive(!toggle);
        }
    }
}