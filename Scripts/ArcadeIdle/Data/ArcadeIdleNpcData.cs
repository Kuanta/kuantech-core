using Kuantech.AI;
using UnityEngine;

namespace Kuantech.ArcadeIdle.Data
{
    [CreateAssetMenu(fileName = "NpcData", menuName = "Kuantech/ArcadeIdle/Npc")]
    public class ArcadeIdleNpcData : ScriptableObject
    {
        public BehaviourTree Bt;
        public ArcadeIdleNpc Prefab;
    }
}