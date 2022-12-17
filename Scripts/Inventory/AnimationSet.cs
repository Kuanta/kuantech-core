using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Animation
{
    [Serializable]
    public struct AnimationClipPair
    {
        public string ClipName;
        public AnimationClip Clip;
    }
    
    public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    {
        public AnimationClipOverrides(int capacity) : base(capacity) {}

        public AnimationClip this[string name]
        {
            get { return this.Find(x => x.Key.name.Equals(name)).Value; }
            set
            {
                int index = this.FindIndex(x => x.Key.name.Equals(name));
                if (index != -1)
                    this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
            }
        }
    }
    
    [CreateAssetMenu]
    public class AnimationSet : ScriptableObject
    {
        public List<AnimationClipPair> MovementClips = new List<AnimationClipPair>();

        public List<AnimationClipPair> MainHandClips = new List<AnimationClipPair>();

        public List<AnimationClipPair> OffHandClips = new List<AnimationClipPair>();

        public void ApplyAnimationSet(AnimatorOverrideController overrideController)
        {
            ApplyMovementSet(overrideController);
            ApplyMainHandClips(overrideController);
            ApplyOffHandClips(overrideController);
        }
        public void ApplyMovementSet(AnimatorOverrideController overrideController)
        {
            ApplyOverrides(overrideController, MovementClips);
        }

        public void ApplyMainHandClips(AnimatorOverrideController overrideController)
        {
            ApplyOverrides(overrideController, MainHandClips);
        }
        
        public void ApplyOffHandClips(AnimatorOverrideController overrideController)
        {
            ApplyOverrides(overrideController, OffHandClips);
        }

        private void ApplyOverrides(AnimatorOverrideController overrideController, List<AnimationClipPair> clipPairs)
        {
            AnimationClipOverrides overrides = new AnimationClipOverrides(clipPairs.Count);
            overrideController.GetOverrides(overrides);
            foreach (var pair in clipPairs)
            {
                if (pair.Clip == null) continue;
                overrides[pair.ClipName] = pair.Clip;
            }
            overrideController.ApplyOverrides(overrides);
        }
        
        public void CopyFrom(AnimationSet set)
        {
            MovementClips.Clear();
            foreach (var pair in set.MovementClips)
            {
                MovementClips.Add(pair);
            }
            
            MainHandClips.Clear();
            foreach (var pair in set.MainHandClips)
            {
                MainHandClips.Add(pair);
            }
            
            OffHandClips.Clear();
            foreach (var pair in set.OffHandClips)
            {
                OffHandClips.Add(pair);
            }
        }

        public void CopyFrom(RuntimeAnimatorController animator)
        {
            MovementClips.Clear();
            MainHandClips.Clear();
            OffHandClips.Clear();
            AnimationClip[] arrclip = animator.animationClips;
            foreach (var clip in arrclip)
            {
                AnimationClipPair pair = new AnimationClipPair()
                {
                    Clip = null,
                    ClipName = clip.name,
                };
                if (clip.name.Contains("Attack"))
                {
                    if (clip.name.Contains("Main"))
                    {
                        MainHandClips.Add(pair);
                    }
                    else
                    {
                        OffHandClips.Add(pair);
                    }
                }
                else
                {
                    MovementClips.Add(pair);
                }
            }
        }
    }
}