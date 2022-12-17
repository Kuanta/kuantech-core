using System;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class Sound
    {
        public AudioSource AudioSource;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(.1f, 3f)] public float Pitch = 1f;
    }
}