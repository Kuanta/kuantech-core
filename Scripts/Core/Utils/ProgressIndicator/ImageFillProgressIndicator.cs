using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// A progress indicator backed by a uGUI Image's fill. The sprite/shader variant next to it drives a
    /// SpriteRenderer, which only exists in world space -- anything living on a Canvas needs this instead.
    /// </summary>
    public class ImageFillProgressIndicator : ProgressIndicator
    {
        [Tooltip("Its Image Type must be set to Filled, or fillAmount does nothing. Whether that reads as " +
                 "a bar or a ring is the Image's own Fill Method and Origin -- a look, not a setting this " +
                 "needs to know about.")]
        [SerializeField] private Image FillImage;

        protected override void ApplyProgress(float progress)
        {
            if (FillImage == null) return;
            FillImage.fillAmount = progress;
        }
    }
}
