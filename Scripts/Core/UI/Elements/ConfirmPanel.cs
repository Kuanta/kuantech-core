using Kuantech.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class ConfirmPanel : UIMenu
    {
        [SerializeField] private Button ConfirmButton;

        public UnityAction OnConfirm = null;
        public void Start()
        {
            ConfirmButton.onClick.AddListener(OnConfirmButton);
        }

        private void OnConfirmButton()
        {
            OnConfirm?.Invoke();
            Close();
        }
    }
}