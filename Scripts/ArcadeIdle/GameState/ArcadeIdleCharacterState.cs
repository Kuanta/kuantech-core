using System;
using Kuantech.Core;
using Kuantech.Utils;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class CharacterState
    {
        [KTTag("CharacterTags")]
        public int WorkerTag;
        public float PosX;
        public float PosZ;
        public float RotY;
        public ActorState ActorState;
    }
}