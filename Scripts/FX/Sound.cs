using System;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class Sound
    {
        public AudioSource AudioSource;
        
        public void Play()
        {
            if(AudioSource == null) return;
            AudioSource.Play();
        }
    }
}