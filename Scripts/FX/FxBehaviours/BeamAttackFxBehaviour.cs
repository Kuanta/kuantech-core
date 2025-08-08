using UnityEngine;

namespace Kuantech.Core.FX
{
    public class BeamAttackFxBehaviour : FxBehaviour
    {
        [SerializeField] private GameObject BeamObject;
        [SerializeField] private Transform EndPoint;
        [SerializeField] private BeamFx BeamFx;
        private CombatModule _caster = null;

        public override void OnFxStarted(Effect parentFx)
        {
            base.OnFxStarted(parentFx);
            _caster = null;
            if (ParentFx == null || ParentFx.EffectPlaySettings.Caster == null) return;
            _caster = ParentFx.EffectPlaySettings.Caster.GetModule<CombatModule>();
            BeamObject.SetActive(true);
        }

        private void Update()
        {
            if (EndPoint == null || _caster == null) return;
            EndPoint.transform.position = _caster.GetTargetPosition();
        }

        public override void OnFxEnded()
        {
            _caster = null;
            BeamObject.SetActive(false);
        }
    }
}