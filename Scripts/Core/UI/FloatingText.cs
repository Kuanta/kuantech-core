using Kuantech.Core;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class FloatingText : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TMP_Text Text;
        [SerializeField] private float LifeTime = 1.5f;
        [SerializeField] private float FadeDelay = 0f;
        [SerializeField] private Vector3 InitialVelocity = new Vector3(0.1f, 1f, 0f);

        private float _timer = 0f;
        private Vector3 _velocity;
        [SerializeField] private Vector3 Acceleration = new Vector3(0, -9.8f, 0);
        [SerializeField] private float AnimaitonSpeedScale = 1f;
        [SerializeField] private bool UsePool;
        private bool _active = false;
        
        public void Fly()
        {
            _active = true;
            _timer = 0f;
            _velocity = InitialVelocity;
            Text.alpha = 1f;
        }

        public void SetText(string text)
        {
            Text.text = text;
        }
        public void SetColor(Color color)
        {
            Text.color = color;
        }

        public void SetGlowColor(Color color)
        {
            Text.fontSharedMaterial.SetColor(ShaderUtilities.ID_GlowColor, color);
        }
        public void SetFontSize(int fontSize)
        {
            Text.fontSize = fontSize;
        }
        
        public bool IsActive()
        {
            return _active;
        }
        
        private void Update()
        {
            if (_timer > LifeTime)
            {
                _active = false;
                if(UsePool) PoolManager.PoolObject(gameObject);
                else Destroy(gameObject);
                return;
            }
            Vector3 newVelocity = _velocity + Acceleration * Time.deltaTime * AnimaitonSpeedScale;
            Vector3 displacement = _velocity * Time.deltaTime * AnimaitonSpeedScale + Acceleration * Time.deltaTime * Time.deltaTime * 0.5f * AnimaitonSpeedScale * AnimaitonSpeedScale;
            transform.localPosition += displacement;
            _velocity = newVelocity;
            _timer += Time.deltaTime;

            if (_timer > FadeDelay)
            {
                Text.alpha = Mathf.Clamp01(1 - _timer / (LifeTime - FadeDelay));
            }
        }

        public void Stop()
        {
            _velocity = Vector3.zero;
        }

        public void SetLifetime(float lifetime)
        {
            _timer = lifetime;
        }
    }
}