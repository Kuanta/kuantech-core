using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.UI
{
    public class KtUIElement : MonoBehaviour
    {
        [Header("UI Element")] 
        [SerializeField] private float CloseDelay = 0f;
        
        //ANimations
        private Animator _animator;
        private static readonly int ShowTrigger = Animator.StringToHash("Open");
        private static readonly int CloseTrigger = Animator.StringToHash("Close");
        private IEnumerator _closeRoutine = null;

        private bool _shown;
        private RectTransform _rectTransform;
        
        //Events
        public UnityAction OnMenuOpened;
        public UnityAction OnMenuClosed;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Opens the ui element and applies the animator
        /// </summary>
        public virtual void Open()
        {
            Show();
            if(_animator != null) _animator.SetTrigger(ShowTrigger);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _shown = true;
            OnMenuOpened?.Invoke();
        }

        public virtual void Close()
        {
            if (!isActiveAndEnabled) return; //Already closed
            if(_animator != null) _animator.SetTrigger(CloseTrigger);
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
            }
            
            _closeRoutine = _CloseRoutine();
            StartCoroutine(_closeRoutine);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            _shown = false;
            OnMenuClosed?.Invoke();
        }
        
        private IEnumerator _CloseRoutine()
        {
            yield return new WaitForSeconds(CloseDelay);
            Hide();
            _closeRoutine = null;
        }

        public bool IsVisible()
        {
            return _shown && gameObject.activeSelf;
        }
        
        #region Helpers

        public bool IsInRect(RectTransform rectTransform, float horizontalBuffer, float verticalBuffer)
        {
            
            return true;
        }
        #endregion
    }
}