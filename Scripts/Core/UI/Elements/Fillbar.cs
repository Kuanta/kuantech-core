using DG.Tweening;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class Fillbar : MonoBehaviour
    {
        private Tween _fillTween;
        public RectTransform FillImage;
        public TMP_Text ValueText;
        public float FillAmount = 1f;

        public float MinAnchoredPos = 0;
        public float MaxAnchoredPos = 1;
        [SerializeField] private Slider _slider;

        public bool ShowMaxValue;
        
        private void Awake()
        {
            if (_slider != null) return;
            _slider = GetComponent<Slider>();
        }

        public void Start()
        {
            SetFill(FillAmount);
            if (FillImage == null) return;
            MaxAnchoredPos = 0;
            MinAnchoredPos = -FillImage.rect.width;
        }

        public void SetFill(float fillAmount, float value)
        {
            SetFill(fillAmount);
            SetValue(value);
        }
        
        public void SetFill(float fillAmount, string value)
        {
            SetFill(fillAmount);
            SetValue(value);
        }
        
        public void SetFill(float fillAmount, float value, float maxValue)
        {
            SetFill(fillAmount);
            SetValue(value, maxValue);
        }
        
        public void SetFill(float fillAmount, string value, string maxValue)
        {
            SetFill(fillAmount);
            SetValue(value, maxValue);
        }
        
        public void SetFill(float fillAmount)
        {
            if (float.IsNaN(fillAmount)) fillAmount = 0f;
            FillAmount = fillAmount;
            if (ValueText != null)
            {
                ValueText.text = "";
            }
            if (_slider != null)
            {
                _slider.value = fillAmount;
                return;
            }
            float newPos = (MaxAnchoredPos - MinAnchoredPos) * fillAmount + MinAnchoredPos;
            if(FillImage != null) FillImage.anchoredPosition = new Vector2(newPos, FillImage.anchoredPosition.y);
            
        }

        public void SetValue(float value)
        {
            if (ValueText != null)
            {
                ValueText.text = value.Stringfy();
            }
        }

        public void SetValue(float value, float maxValue)
        {
            if (ValueText == null) return;
            ValueText.text = $"{value.Stringfy()} / {maxValue.Stringfy()}";
            SetFill(value/maxValue);
        }

        public void SetValue(string value)
        {
            if (ValueText != null)
            {
                ValueText.text = value;
            }
        }

        public void SetValue(string value, string maxValue)
        {
            if (ValueText != null)
            {
                ValueText.text = $"{value} / {maxValue}";
            }
        }

        #region Animated Fill

        /// <summary>Tweens the fill toward target over duration (instant if duration &lt;= 0).</summary>
        public Tween AnimateFill(float target, float duration, Ease ease = Ease.OutCubic)
        {
            _fillTween?.Kill();
            if (duration <= 0f)
            {
                SetFill(target);
                return null;
            }
            _fillTween = BuildFillTween(FillAmount, target, duration, ease);
            return _fillTween;
        }

        /// <summary>
        /// Like AnimateFill, but if the fill would drop (a bar that wrapped, e.g. an XP level-up) it first
        /// fills to full, snaps to empty, then fills to the new amount.
        /// </summary>
        public Tween AnimateFillWrap(float target, float duration, Ease ease = Ease.OutCubic)
        {
            _fillTween?.Kill();
            if (duration <= 0f)
            {
                SetFill(target);
                return null;
            }

            if (target < FillAmount - 0.001f)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(BuildFillTween(FillAmount, 1f, duration * 0.5f, ease));
                seq.AppendCallback(() => SetFill(0f));
                seq.Append(BuildFillTween(0f, target, duration * 0.5f, ease));
                _fillTween = seq;
            }
            else
            {
                _fillTween = BuildFillTween(FillAmount, target, duration, ease);
            }
            return _fillTween;
        }

        public void KillFillTween()
        {
            _fillTween?.Kill();
            _fillTween = null;
        }

        private Tween BuildFillTween(float from, float to, float duration, Ease ease)
        {
            float value = from;
            return DOTween.To(() => value, x =>
                {
                    value = x;
                    SetFill(x);
                }, to, duration)
                .SetEase(ease);
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
        }

        #endregion
    }
}