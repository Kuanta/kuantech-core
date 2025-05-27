namespace Kuantech.Core.FX
{
    /// <summary>
    /// An effects handler class for the actor
    /// </summary>
    public class ActorEffectsModule : ActorModule
    {
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnHit;
        }
        
        #region Events
        private void OnHit(HitInfo info)
        {
            //Play hit effect     
            ActorVisual visual = Actor.VisualHandler.GetActorVisual();
            if (visual != null && visual.HitShaderEffect != null)
            {
                visual.HitShaderEffect.PlayShaderEffect();
            }
        }
        #endregion
    }
}