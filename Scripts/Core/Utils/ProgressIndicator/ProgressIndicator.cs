using UnityEngine;

namespace Kuantech.Core.Utils
{
    public abstract class ProgressIndicator : MonoBehaviour
    {
        public float SmoothFactor = 1;
        private float _currentProgress = 0f;
        private float _targetProgress = 0f;
        
        public void SetProgress(float progresss, bool immediate = false)
        {
            progresss = Mathf.Clamp01(progresss);
            if (immediate)
            {
                _currentProgress = progresss;
            }
            _targetProgress = progresss;
        }

        private void Update()
        {
            UpdateProgress();    
        }
        
        protected virtual void UpdateProgress()
        {
            _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime* SmoothFactor);
            ApplyProgress(_currentProgress);
        }

        protected abstract void ApplyProgress(float progress);
    }
}