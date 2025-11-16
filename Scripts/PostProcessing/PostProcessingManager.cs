using Kuantech.Core;
using UnityEngine.Rendering;

namespace Kuantech.PostProcessing
{
    public class PostProcessingManager : SubManager
    {
        public Volume MainVolume;

        public static Volume GetMainVolume()
        {
            var ctx = PostProcessingManager.GetContext<PostProcessingManager>();
            if (ctx == null) return null;
            return ctx.MainVolume;
        }
    }
}