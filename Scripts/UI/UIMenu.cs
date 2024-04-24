using System.Collections;
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
                CloseButton.onClick.AddListener(Close);
            }
            
            //Get animator
        }
        protected virtual void Start()
        {
           Initialize();
        }
        public virtual void Show()
        {
            Initialize();
            if(_animator != null) _animator.SetTrigger(ShowTrigger);
            gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            if (!isActiveAndEnabled) return; //Already closed
            if(_animator != null) _animator.SetTrigger(ShowTrigger);
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
            }

            _closeRoutine = _CloseRoutine();
            StartCoroutine(_closeRoutine);
        }

        private IEnumerator _CloseRoutine()
        {
            yield return new WaitForSeconds(CloseDelay);
            gameObject.SetActive(false);
            _closeRoutine = null;
        }
    }
}