using System;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("ActorState")] public ActorSerializableData actorSerializableData;
    }
}