using System.Collections;
using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class UIMenu : MonoBehaviour
    {
        public string MenuId;
        [SerializeField] protected Button CloseButton;

        [SerializeField] private float CloseDelay = 0f;
        
        //Animations
        private Animator _animator;
        private bool _initialized = false;
        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int CloseTrigger = Animator.StringToHash("Close");

        private IEnumerator _closeRoutine = null;
        protected virtual void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    UIManager.GetContext<UIManager>().PopFromStack(this);
                });
            }
            
            //Get animator
        }
        protected virtual void Start()
        {
           Initialize();
        }
        public virtual void Open()
        {
            Initialize();
            if(_animator != null) _animator.SetTrigger(ShowTrigger);
            Show();
            UIManager.GetContext<UIManager>().PushToStack(this, false); //Don't call open again
        }

        public virtual void Close()
        {
            if (UIManager.GetTopMenu() != this)
            {
                Debug.LogWarning($"Menu {MenuId} tried to close itself while not being on top of stack.");
                return;
            }
            if (!isActiveAndEnabled) return; //Already closed
            if(_animator != null) _animator.SetTrigger(ShowTrigger);
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
            }

            _closeRoutine = _CloseRoutine();
            StartCoroutine(_closeRoutine);
            UIManager.GetContext<UIManager>().PopFromStack(this, false); //Don't call close again
        }

        private IEnumerator _CloseRoutine()
        {
            yield return new WaitForSeconds(CloseDelay);
            Hide();
            _closeRoutine = null;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}