using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public struct ActorSpawnData
    {
        public string ActorId;
        public string ActorPrefabId;
        public string ActorVisualId;
        public int FactionId;

        [SerializeReference]
        public List<ActorModuleSerializableData> ModuleDatas;

        public readonly bool IsValid() => !string.IsNullOrEmpty(ActorId);
    }
}