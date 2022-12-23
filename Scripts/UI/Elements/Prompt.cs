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

        [SerializeField] private UnityAction ConfirmHandler;
        
        private void Awake()
        {
            ConfirmButton.onClick.AddListener((() =>
            {
                ConfirmHandler?.Invoke();
                Close();
            }));
            
            CancelButton.onClick.AddListener(Close);
        }

        public void SetListener(UnityAction handler)
        {
            ConfirmHandler = handler;
        }
    }
}