using UnityEngine;

namespace Kuantech.Core.FX
{
    public class LightningFxBehaviour: FxBehaviour
    {
        [SerializeField] private GameObject LightningObject;
        [SerializeField] private Transform StartPoint;
        [SerializeField] private Transform EndPoint;
        [SerializeField] private float PointsMoveSpeed = 100f;
        [SerializeField] private float StartPointMoveStartDelay = 0.2f;
        private CombatModule _caster = null;
        protected override void OnFxStarted(Effect parentFx)
        {
            base.OnFxStarted(parentFx);
            _caster = null;
            if (ParentFx == null || ParentFx.EffectPlaySettings.Caster == null) return;
            _caster = ParentFx.EffectPlaySettings.Caster.GetModule<CombatModule>();
            StartPoint.transform.position = parentFx.transform.position;
            EndPoint.transform.position = parentFx.transform.position;
            LightningObject.SetActive(true);
        }

        public override void UpdateFx()
        {
            base.UpdateFx();
            if (EndPoint == null || _caster == null || !_behaviourStarted) return;
            EndPoint.transform.position = Vector3.MoveTowards(EndPoint.transform.position, GetTargetPosition(), PointsMoveSpeed * Time.deltaTime);
            if (Time.time - _fxStartTime > StartPointMoveStartDelay)
            {
                StartPoint.transform.position = Vector3.MoveTowards(StartPoint.transform.position, GetTargetPosition(), PointsMoveSpeed * Time.deltaTime);
            }
        }

        public override void OnFxEnded()
        {
            _caster = null;
            LightningObject.SetActive(false);
        }

        private Vector3 GetTargetPosition()
        {
            return ParentFx.EffectPlaySettings.PlayEndPoint.GetTargetPosition();
        }
    }
}