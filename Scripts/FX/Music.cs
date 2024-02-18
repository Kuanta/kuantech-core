using System;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public class Music
    {
            public string Id;
            public AudioClip Clip;
            public bool Loop;
            public float Volume;
    }
}