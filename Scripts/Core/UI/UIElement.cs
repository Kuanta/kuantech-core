using System.Collections;
using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.UI
{
    public class UIElement : MonoBehaviour
    {
        [Header("UI Element")] [SerializeField]
        private bool UseTimeScale = false;
        [SerializeField] private float CloseDelay = 0f;
        [SerializeField] private Effect OnShowEffect;
        
        //ANimations
        protected Animator ElementAnimator;
        private static readonly int ShowTrigger = Animator.StringToHash("Open");
        private static readonly int CloseTrigger = Animator.StringToHash("Close");
        private IEnumerator _closeRoutine = null;
        
        protected bool Initialized = false;
        private bool _shown;
        private RectTransform _rectTransform;

        //Events
        public UnityAction OnMenuOpened;
        public UnityAction OnMenuClosed;

        public virtual void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
            ElementAnimator = GetComponent<Animator>();
            if (ElementAnimator != null)
            {
                ElementAnimator.logWarnings = false;
                if(!UseTimeScale) ElementAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
            _rectTransform = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// Opens the ui element and applies the animator
        /// </summary>
        public virtual void Open()
        {
            Show();
            if(ElementAnimator != null) ElementAnimator.SetTrigger(ShowTrigger);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _shown = true;
            if(OnShowEffect != null) OnShowEffect.Play();
            OnMenuOpened?.Invoke();
        }

        public virtual void Close()
        {
            if (!isActiveAndEnabled) return; //Already closed
            if(ElementAnimator != null) ElementAnimator.SetTrigger(CloseTrigger);
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
            yield return new WaitForSecondsRealtime(CloseDelay); //Wait for seconds without time scale
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