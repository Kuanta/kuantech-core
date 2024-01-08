using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class ProgressBar : MonoBehaviour {
        [SerializeField] private Image FillImage;
        [SerializeField] private SpriteRenderer SpriteRenderer;

        public void SetFill(float normalized)
        {
            if(FillImage != null){
                FillImage.fillAmount = normalized;
            }else if(SpriteRenderer != null)
            {
                SpriteRenderer.material.SetFloat("_FillAmount", normalized);
            }
        }
    }
}