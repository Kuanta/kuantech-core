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
        private Slider _slider;

        public bool ShowMaxValue;
        
        private void Awake()
        {
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
        
        public void SetFill(float fillAmount, float value, float maxValue)
        {
            SetFill(fillAmount);
            SetValue(value, maxValue);
        }
        
        public void SetFill(float fillAmount)
        {
            FillAmount = fillAmount;
            if (_slider != null)
            {
                _slider.value = fillAmount;
                return;
            }
            float newPos = (MaxAnchoredPos - MinAnchoredPos) * fillAmount + MinAnchoredPos;
            FillImage.anchoredPosition = new Vector2(newPos, FillImage.anchoredPosition.y);
        }

        public void SetValue(float value)
        {
            if (ValueText != null)
            {
                ValueText.text = value.ToString();
            }
        }

        public void SetValue(float value, float maxValue)
        {
            if (ValueText != null)
            {
                ValueText.text = $"{value} / {maxValue}";
            }
        }
    }
}