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
        
        [Header("Play On Actor")]
        public string SlotToPlay;

        /// <summary>
        /// Plays the effect
        /// </summary>
        [Button("Play Effect")]
        public void PlayEffect(Actor actor=null)
        {
            Vector3 playPosition = transform.position;
            Quaternion playRotation = transform.rotation;
            if (actor != null)
            {
                ActorSlotsHandler slotHandler = actor.GetModule<ActorSlotsHandler>();
                if (slotHandler != null)
                {
                    
                }
            }
            EffectPlayer.PlayEffectAtPosition(transform.position, transform.rotation);
        }
        
    }
}