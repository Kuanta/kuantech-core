using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// Effect player in the form of a component
    /// </summary>
    public class EffectPlayerComponent : MonoBehaviour
    {
        public EffectPlayer EffectPlayer;
        public bool AdjustPosition = true; //If true, the effect will be played at the position of the actor
        [Header("Play On Actor")]
        public string SlotToPlay;

        /// <summary>
        /// Plays the effect
        /// </summary>
        [Button("Play Effect")]
        public Effect PlayEffect(EffectPlaySettings playSettings)
        {
            Vector3 playPosition = transform.position;
            Quaternion playRotation = transform.rotation;
            // if (plactor != null)
            // {
            //     ActorSlotsHandler slotHandler = actor.GetModule<ActorSlotsHandler>();
            //     if (slotHandler != null)
            //     {
            //         
            //     }
            // }
            playSettings.SetPosition = AdjustPosition;
            return EffectPlayer.PlayEffect(playSettings);
        }
        
    }
}