using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class CombatIndicator : MonoBehaviour
    {
        public struct CombatIndicatorData
        {
            public float Duration;
            public float Range;
            public float Width;
            public float Angle;
            public Vector3 Direction;
            public Vector3 Position;
            public bool SetPosition;
        }

        private float _startShowTime;
        private bool _started;
        private float _duration;
        public void Show(CombatIndicatorData indicatorData)
        {
            gameObject.SetActive(true);
            _started = true;
            _startShowTime = Time.time;
            _duration = Mathf.Max(0.01f, indicatorData.Duration);

            if (indicatorData.SetPosition)
            {
                transform.position = indicatorData.Position;

                Vector3 dir = indicatorData.Direction;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            Setup(indicatorData);
        }

        protected virtual void Setup(CombatIndicatorData data) { }

        private void Update()
        {
            if(!_started) return;

            float normalizedTime = GetNormalizedTime();
            SetFill(normalizedTime);
            if(normalizedTime >= 1)
            {
                EndIndicator();
            }
        }

        public void EndIndicator()
        {
            _started = false;
            gameObject.SetActive(false);
        }

        protected virtual void SetFill(float fill)
        {
            
        }

        private float GetNormalizedTime()
        {
            return Mathf.Clamp01((Time.time - _startShowTime) / _duration);
        }
    }
}