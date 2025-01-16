using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    public class ComboIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text ComboCounterText;
        [SerializeField] private string ComboTextPrefix = "x";
        [SerializeField] private Animator Animator;
        [SerializeField] private float FadeOutDuration;
        private static readonly int Combo = Animator.StringToHash("Combo");
        private static readonly int Out = Animator.StringToHash("FadeOut");

        private float _lastComboTime;
        private bool _fadingOut = false;

        public void TriggerCombo(int comboCount)
        {
            if (comboCount == 0) return;
            _fadingOut = false;
            Animator.Rebind();
            gameObject.SetActive(true);
            _lastComboTime = Time.time;
            SetComboCounter(comboCount + 1);
            Animator.SetTrigger(Combo);

        }
        public void SetComboCounter(int comboCount)
        {
            ComboCounterText.text = $"{ComboTextPrefix}{comboCount.ToString()}";;
        }

        private void Update()
        {
            if (_fadingOut) return;
            float timeDiff = Time.time - _lastComboTime;
            if (timeDiff >= FadeOutDuration)
            {
                FadeOut();
            }
        }

        private void FadeOut()
        {
            _fadingOut = true;
            Animator.SetTrigger(Out);
        }

        private void OnFadeOutEnd()
        {
            _fadingOut = false;
            gameObject.SetActive(false);
            Animator.Rebind();
        }
    }
}