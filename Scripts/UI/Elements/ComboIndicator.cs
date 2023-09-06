using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    public class ComboIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text ComboCounterText;
        [SerializeField] private Animator Animator;
        private static readonly int Combo = Animator.StringToHash("Combo");

        public void SetComboCounter(int comboCount)
        {
            ComboCounterText.text = $"x {comboCount.ToString()}";;
            if (comboCount == 0) return;
            Animator.SetTrigger(Combo);
        }
    }
}