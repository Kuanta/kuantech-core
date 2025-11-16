using DG.Tweening;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.FX
{
    public class ParticleSystemColorChangeBehaviour : FxBehaviour
    {
        public ParticleSystem ParticleSystem;
        
        [ColorUsage(true, true)]
        public Color StartColor;
        [ColorUsage(true, true)]
        public Color EndColor;
        
        [Header("Timing")]
        public float Change = 0.5f;
        public bool UseUnscaledTime = false;
        public Ease Ease = Ease.Linear;
        
        public string ColorProperty = "_BaseColor";
        
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _colorId;
        private Tweener _tween;
        
        protected override void OnFxStarted(Effect parentFx)
        {
            KillTween();
            
            //Change color of particle system renderers _BaseColor from StartColor to EndColor
            if (!ParticleSystem) return;
            _renderer ??= ParticleSystem.GetComponent<Renderer>();
            if (!_renderer) return;
            
            _mpb ??= new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(ColorProperty);
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, StartColor);
            _renderer.SetPropertyBlock(_mpb);
            
            // --- PARTICLE START COLOR TWEEN ---
            var main = ParticleSystem.main;

            var start = main.startColor;

            _tween = DOTween.To(
                    () => start.color,                     // getter
                    col => { var sc = main.startColor; sc.color = col; main.startColor = sc; }, // setter
                    EndColor,                              // target
                    Change                                 // duration
                )
                .From(StartColor)
                .SetEase(Ease)
                .SetUpdate(UseUnscaledTime);
        }
        
        public override void OnFxEnded()
        {
            KillTween();
        }

        private void KillTween()
        {
            if (_tween == null) return;
            _tween?.Kill();
            _tween = null;
        }
    }
}