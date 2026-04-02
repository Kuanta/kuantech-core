using Kuantech.Core.Camera;

namespace Kuantech.Core.FX
{
    public class CameraShakeFx : FxBehaviour
    {
        public float ShakeDuration = 0.5f;
        public float ShakeMagnitude = 0.5f;
        public int Vibrato = 5;
        
        protected override void OnFxStarted(Effect parentFx)
        {
            KtCamera ktCamera = CameraManager.GetKtCamera();
            ktCamera.ShakeCamera(ShakeMagnitude,ShakeDuration, Vibrato);
        }
    }
}