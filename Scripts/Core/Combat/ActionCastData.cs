using System;
using UnityEngine;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Core
{
    [Serializable]

    public class ActionCastData
#if NETWORKING_NGO
        : INetworkSerializable
#endif
    {
        public Actor Caster;
        public Vector3 StartPosition; //Start position of the cast
        public Vector3 Direction; //Direction of the cast
        public Vector3 TargetPosition; //Targeted position
        public Actor Target; //Targeted actor
        public bool OverrideRotation = true;

        /// <summary>
        /// Optional: re-evaluated every tick by a channeled behaviour (see SkillBehaviour.GetLiveDirection)
        /// to aim at whatever "the current target" means to whoever built this cast data — a player's live
        /// closest-enemy, a cursor position, anything. Lets each caster (AutoCastModule, manual test-cast,
        /// ...) define that meaning itself, without ActionCastData/SkillBehaviour knowing about any of them.
        /// Leave null to fall back to the frozen Target/TargetPosition, same as before this existed.
        /// </summary>
        public Func<Vector3> LiveAimPointProvider;

        public Vector3 GetCastPoint()
        {
            if (Target != null) return Target.transform.position;
            return TargetPosition;
        }

#if NETWORKING_NGO
        // Caster and LiveAimPointProvider never cross the wire -- Caster is always implicit (whichever
        // CombatModule/SpellBook is sending this), and a delegate can't be serialized at all. The receiving
        // side is expected to fill Caster in itself from context, same as it always resolved "who's
        // attacking" from the RPC's own sender rather than trusting a passed-in reference.
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StartPosition);
            serializer.SerializeValue(ref Direction);
            serializer.SerializeValue(ref TargetPosition);
            serializer.SerializeValue(ref OverrideRotation);

            NetworkObjectReference targetRef = default;
            if (!serializer.IsReader && Target != null)
            {
                NetworkObject targetNetObj = Target.GetComponent<NetworkObject>();
                if (targetNetObj != null) targetRef = targetNetObj;
            }
            serializer.SerializeValue(ref targetRef);
            if (serializer.IsReader && targetRef.TryGet(out NetworkObject readNetObj))
                Target = readNetObj.GetComponent<Actor>();
        }
#endif
    }
}