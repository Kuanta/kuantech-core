using Kuantech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class Prompt : UIMenu
    {
        [SerializeField] protected Button ConfirmButton;
        [SerializeField] protected Button CancelButton;
        [SerializeField] private TMP_Text PromptText;
        
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

        public override void Show()
        {
            base.Show();
            GameManager.Instance.PauseGame();
        }

        public override void Close()
        {
            base.Close();
            GameManager.Instance.ResumeGame();
        }
        
        public void SetConfirmListener(UnityAction handler)
        {
            ConfirmHandler = handler;
        }

        public void SetCloseListener(UnityAction handler)
        {
            CloseHandler = handler;
        }

        public void SetPromptText(string text)
        {
            if (PromptText == null) return;
            PromptText.text = text;
        }
        
    }
}