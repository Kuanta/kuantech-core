using Kuantech.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class Prompt : UIMenu
    {
        [SerializeField] protected Button ConfirmButton;
        [SerializeField] protected Button CancelButton;

        private UnityAction ConfirmHandler;
        private UnityAction CloseHandler;
        
        private void Awake()
        {
            ConfirmButton.onClick.AddListener((() =>
            {
                ConfirmHandler?.Invoke();
                Close();
            }));
            
            CancelButton.onClick.AddListener((() =>
            {
                CloseHandler?.Invoke();
                Close();
            }));
        }

        public void SetConfirmListener(UnityAction handler)
        {
            ConfirmHandler = handler;
        }

        public void SetCloseListener(UnityAction handler)
        {
            CloseHandler = handler;
        }
        
    }
}