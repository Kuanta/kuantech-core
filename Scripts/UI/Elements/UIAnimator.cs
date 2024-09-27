using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    /// <summary>
    /// A class to handle animations for ui elements. For now it supports a single element
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        [Header("Animator")] 
        [SerializeField] private Animator Animator;

        private static readonly int Animate = Animator.StringToHash("Animate");
        
        //Events
        public UnityAction OnAnimationEnd;
        
        // public struct AnimationClipEntry
        // {
        //     [KTTag("AnimationTag")]
        //     public int AnimatonTag;
        //     
        //     public string TriggerName;
        // }
        //
        // [Header("Animator")] 
        // [SerializeField] private Animator Animator;
        //
        // public List<AnimationClipEntry> Clips;
        // private Dictionary<int, AnimationClipEntry> _clipsMap;

        public void PlayAnimation()
        {
            if (!gameObject.activeInHierarchy) return;
            if (Animator == null) return;
            Animator.SetTrigger(Animate);
        }

        public void Reset()
        {
            if (Animator == null) return;
            Animator.Rebind();
        }
        
        /// <summary>
        /// Animation event handler
        /// </summary>
        public void OnAnimationEndHandler()
        {
            
        }
    }
}