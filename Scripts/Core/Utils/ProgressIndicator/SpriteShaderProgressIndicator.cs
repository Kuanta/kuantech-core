using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// A progress in dicator that uses a sprite shader to display progress.
    /// </summary>
    public class SpriteShaderProgressIndicator : ProgressIndicator
    {
        public string ShaderPropertyName = "_Progress";
        public SpriteRenderer Sprite;
        
        protected override void ApplyProgress(float progress)
        {
            Sprite.material.SetFloat(ShaderPropertyName, progress);
        }
    }
}