using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// Marks what this object is made of (stone, wood, flesh, ...) so hit effects can pick a matching
    /// variant -- see Effect.SurfaceVariants. Put it on anything whose material should sound/look different
    /// when struck: a wooden door, a metal gate, an enemy. Everything else is covered by EffectsLibrary's
    /// layer mapping, so only the exceptions need this component.
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        [KTTag("SurfaceTag")]
        [Tooltip("Which surface this object counts as. Tag names come from the SurfaceTag group in " +
                 "Assets/Kuantech/Settings/KTTags.asset.")]
        public int Surface;
    }

    /// <summary>
    /// Resolves "what did I just hit" down to a single surface tag. Lookup order: an explicit SurfaceTag on
    /// the collider or any of its parents (so a tag on an actor root covers all its hitboxes), then
    /// EffectsLibrary's layer mapping, then Default.
    /// </summary>
    public static class SurfaceTags
    {
        /// <summary>Tag 0 -- whatever a surface falls back to when nothing else matches.</summary>
        public const int Default = 0;

        /// <summary>
        /// Whether this project uses surface-aware effects at all. Off unless EffectsLibrary says otherwise
        /// (and off entirely when there is no EffectsLibrary), so every Resolve below answers Default and
        /// every effect keeps playing its own vfx/sfx -- the whole feature is an opt-in addition.
        /// </summary>
        public static bool IsEnabled()
        {
            // InstanceExists, not Instance: the singleton getter CREATES a GameManager when none exists, and a
            // scene that never had one must not grow one just because something got hit.
            if (!GameManager.InstanceExists()) return false;
            EffectsLibrary library = EffectsLibrary.GetContext<EffectsLibrary>();
            return library != null && library.EnableSurfaceEffects;
        }

        public static int Resolve(Collider collider)
        {
            return collider != null ? Resolve(collider.gameObject) : Default;
        }

        public static int Resolve(GameObject target)
        {
            if (target == null || !IsEnabled()) return Default;

            SurfaceTag tag = target.GetComponentInParent<SurfaceTag>();
            if (tag != null) return tag.Surface;

            return EffectsLibrary.GetSurfaceForLayer(target.layer);
        }
    }
}
