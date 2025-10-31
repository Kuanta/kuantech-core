namespace Kuantech.Core.FX
{
    public class HitStopFxBehaviour : FxBehaviour
    {
        public float HitStopDuration = 0.1f;
        public float HitStopTimeScale = 0.2f;

        protected override void OnFxStarted(Effect parentFx)
        {
            CombatManager cm = CombatManager.GetContext<CombatManager>();
            if (cm == null) return;
            cm.PushHitStop(HitStopTimeScale, HitStopDuration);
        }
    }
}