using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class Fillbar : MonoBehaviour
    {
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
            if (ValueText != null)
            {
                ValueText.text = $"{value.Stringfy()} / {maxValue.Stringfy()}";
            }
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
    }
}