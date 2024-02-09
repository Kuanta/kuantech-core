using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class ToggleButton : MonoBehaviour
    {
        public Button Button;
        [SerializeField] private GameObject OnImage;
        [SerializeField] private GameObject OffImage;
        
        public bool State;
        public UnityAction<bool> OnToggle;

        private void Awake()
        {
            Button.onClick.AddListener(OnButtonPress);
        }

        private void OnButtonPress()
        {
            SetState(!State);
        }

        public void SetState(bool toggle)
        {
            if(OnImage != null) OnImage.SetActive(toggle);
            if(OffImage != null) OffImage.SetActive(!toggle);
            State = toggle;
            OnToggle?.Invoke(State);
        }
    }
}