using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// A place a hit can land: a head, a torso, an arm. Registered on CombatManager and looked up by id,
    /// exactly the way attack patterns and damage types already are.
    ///
    /// An asset rather than an enum for two reasons. Balance lives in one place -- the head is worth its
    /// multiplier once here, not on every rig that has one -- and this is where everything else that is
    /// body-part-specific ends up hanging as it arrives: gore variants, dismemberment, armour. An enum
    /// would have to be edited, and every switch over it revisited, for each of those.
    ///
    /// MetadataAsset already carries Id/Name/Icon/MainColor, so a headshot gets its own damage-text colour
    /// and HUD icon without a single extra field.
    /// </summary>
    [CreateAssetMenu(fileName = "BodyPartAsset", menuName = "Kuantech/Combat/Body Part")]
    public class BodyPartAsset : MetadataAsset
    {
        [Tooltip("Damage landing on this part is multiplied by this. 1 leaves it alone -- a head is worth " +
                 "more than a torso, a limb usually less. This is the number you balance headshots with.")]
        public float DamageMultiplier = 1f;
    }
}
