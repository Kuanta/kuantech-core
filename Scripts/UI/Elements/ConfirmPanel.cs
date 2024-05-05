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
        protected override void Start()
        {
            base.Start();
            ConfirmButton.onClick.AddListener(OnConfirmButton);
        }

        private void OnConfirmButton()
        {
            OnConfirm?.Invoke();
            Close();
        }
    }
}