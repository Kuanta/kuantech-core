using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Kuantech.Core;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech.UI
{
    public class FloatingText : MonoBehaviour
    {
        public RectTransform RectTransform;
        [SerializeField] private TMP_Text Text;
        [SerializeField] private float LifeTime = 1.5f;
        [SerializeField] private Vector3 StartLocalPosition = new Vector3(0, 0f, 0f);
        [SerializeField] private Vector3 InitialVelocity = new Vector3(0.1f, 1f, 0f);

        private float _timer = 0f;
        private Vector3 _velocity;
        [SerializeField] private Vector3 Acceleration = new Vector3(0, -9.8f, 0);
        [SerializeField] private float AnimaitonSpeedScale = 1f;

        private Vector3 _initialLocalPosition;
        private bool _active = false;

        private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;
        
        public void Initialize(string text, float speed = 0.5f)
        {
            _active = true;
            _timer = 0f;
            Text.text = text;
            _initialLocalPosition = RectTransform.position + new Vector3(Random.Range(-50f, 50f), 0f, 0f);
            RectTransform.position = _initialLocalPosition;
            InitialVelocity.x = Random.Range(-speed, speed);
            _velocity = InitialVelocity;
            RectTransform.localScale = Vector3.one * 0.7f;
            _scaleTween = transform.DOScale(Vector3.one, 0.5f);
        }

        public void SetText(string text)
        {
            Text.text = text;
        }
        public void SetColor(Color color)
        {
            Text.color = color;
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
                GameManager.Instance.Pool.PoolObject(gameObject);
            }
            Vector3 newVelocity = _velocity + Acceleration * Time.deltaTime * AnimaitonSpeedScale;
            Vector3 displacement = _velocity * Time.deltaTime * AnimaitonSpeedScale + Acceleration * Time.deltaTime * Time.deltaTime * 0.5f * AnimaitonSpeedScale * AnimaitonSpeedScale;
            transform.localPosition += displacement;
            _velocity = newVelocity;
            _timer += Time.deltaTime;
        }

        public void Reset()
        {
            if(_scaleTween != null) _scaleTween.Kill();
            RectTransform.localScale = Vector3.one;
            _timer = 0f;
            transform.position = _initialLocalPosition;
            _velocity = InitialVelocity;
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