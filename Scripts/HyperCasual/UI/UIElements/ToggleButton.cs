using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class ToggleButton : MonoBehaviour
    {
        public Button Button;
        [SerializeField] private Image OnImage;
        [SerializeField] private Image OffImage;
        
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
            OnImage.gameObject.SetActive(toggle);
            OffImage.gameObject.SetActive(!toggle);
            State = toggle;
            OnToggle?.Invoke(State);
        }
    }
}