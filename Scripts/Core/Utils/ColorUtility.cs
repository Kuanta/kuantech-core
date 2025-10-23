using UnityEngine;

namespace Kuantech.Utils
{
    public class ColorUtility
    {
        public static Gradient CreateWarmCoolGradient(Color baseColor, float variation = 0.1f)
        {
            // Soğuk renk: biraz mavi/cyan tonuna kaydır
            Color coolColor = new Color(
                Mathf.Clamp01(baseColor.r - variation * 0.5f),
                Mathf.Clamp01(baseColor.g + variation * 0.2f),
                Mathf.Clamp01(baseColor.b + variation)
            );

            // Sıcak renk: biraz kırmızı/sarı tonuna kaydır
            Color warmColor = new Color(
                Mathf.Clamp01(baseColor.r + variation),
                Mathf.Clamp01(baseColor.g + variation * 0.3f),
                Mathf.Clamp01(baseColor.b - variation * 0.5f)
            );

            // Gradient oluştur
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(coolColor, 0f),
                    new GradientColorKey(baseColor, 0.5f),
                    new GradientColorKey(warmColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            return gradient;
        }
    }
}