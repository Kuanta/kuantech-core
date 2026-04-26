using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.UI
{
    public class ToggleButton : MonoBehaviour
    {
        public KtButton Button;
        [SerializeField] private GameObject OnImage;
        [SerializeField] private GameObject OffImage;
        
        public bool State;
        public UnityAction<bool> OnToggle;

        [Header("Settings Manager")] 
        public string SettingKey;
        
        public void Initialize()
        {
            bool currentState = SettingsManager.GetBoolSetting(SettingKey, false);
            SetState(currentState, false);
            Button.onClick.AddListener(OnButtonPress);
        }
        
        private void OnButtonPress()
        {
            SetState(!State);
            if(Button == null) return;
            if(State)
            {
                Button.TriggerPositiveEffect();
            }
            else
            {
                Button.TriggerNegativeEffect();
            }
        }

        public void SetState(bool toggle, bool fireEvent = true)
        {
            if(OnImage != null) OnImage.SetActive(toggle);
            if(OffImage != null) OffImage.SetActive(!toggle);
            State = toggle;
            if (fireEvent)
            {
                OnToggle?.Invoke(State);
                if (!SettingKey.IsNullOrEmpty())
                {
                    SettingsManager.SetBoolSetting(SettingKey, toggle);
                }
            }
        }
    }
}