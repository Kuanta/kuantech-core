using TMPro;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text FpsText;
        private float[] _deltaTimes;
        private int _lastFrameIndex = 0;

        private void Awake()
        {
            _deltaTimes = new float[50];
        }

        private void Update()
        {
            _deltaTimes[_lastFrameIndex] = Time.deltaTime;
            _lastFrameIndex = (_lastFrameIndex + 1) % _deltaTimes.Length;

            FpsText.text = "Fps:" + Mathf.Ceil(CalculateFPS());
        }

        private float CalculateFPS()
        {
            float total = 0f;
            foreach (var deltaTime in _deltaTimes)
            {
                total += deltaTime;
            }

            if (total == 0) return 1000f;
            return _deltaTimes.Length / total; //Inverse is the fps
        }
    }
}