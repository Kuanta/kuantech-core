using Kuantech.Core.Combat;
using Kuantech.Rpg;
using Kuantech.Rpg.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
#if NETWORKING_NGO
using Unity.Netcode;
#endif

namespace Kuantech.Core
{
    /// <summary>
    /// A DTO for damage info
    /// </summary>
    [Serializable]
    public struct DamageInfo
#if NETWORKING_NGO
        : INetworkSerializable
#endif
    {
        public DamageType DamageType; //Type of damge
        public float DamageAmount; //Amount of damage
        public bool IsCritical; //If is critical, useful for UI
        // Defaults to false, i.e. shown -- opt OUT of combat text per hit, instead of having to opt every
        // single damage source in (which is how most skills ended up never showing any text at all).
        public bool HideDamageText;

        public float GetDamage()
        {
            return DamageAmount;
        }

        public void SetDamage(float damage)
        {
            DamageAmount = damage;
        }

#if NETWORKING_NGO
        // DamageType is a MetadataAsset (ScriptableObject) -- can't cross the wire itself, so it travels as
        // its string id and gets resolved back through RpgManager on the other end, same as every other
        // asset-by-id lookup already in this codebase (see ObserverSyncResource_Rpc).
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            string damageTypeId = !serializer.IsReader ? (DamageType != null ? DamageType.GetId() : string.Empty) : string.Empty;
            serializer.SerializeValue(ref damageTypeId);
            if (serializer.IsReader)
                DamageType = string.IsNullOrEmpty(damageTypeId) ? null : RpgManager.GetDamageTypeById(damageTypeId);

            serializer.SerializeValue(ref DamageAmount);
            serializer.SerializeValue(ref IsCritical);
            serializer.SerializeValue(ref HideDamageText);
        }
#endif
    }

    [Serializable]
    public struct HitInfo
#if NETWORKING_NGO
        : INetworkSerializable
#endif
    {
        public GameObject Hitter;
        public DamageInfo DamageInfo;
        public List<DamageInfo> AdditionalDamages;
        public Vector3 HitDirection;
        public float KnockbackForce;
        public float KnockbackDuration;

        /// <summary>
        /// Where on the body this landed, or null when the hit resolved against an actor with no parts
        /// authored -- which is every actor until hitboxes are placed on its rig, so null has to stay a
        /// perfectly ordinary answer.
        ///
        /// The damage in DamageInfo already has the part's multiplier baked in; this is carried separately
        /// for everything that needs to know WHERE rather than how much: a headshot's own sound, a different
        /// gore variant, the damage text taking the part's colour.
        /// </summary>
        public BodyPartAsset HitBodyPart;

#if NETWORKING_NGO
        // Hitter is a GameObject -- can't serialize a GameObject reference directly, it has to be the
        // NetworkObject sitting on it. AdditionalDamages has no built-in List<T> support for a custom
        // INetworkSerializable element type, so it's a manual length-prefixed loop (DamageInfo's own
        // NetworkSerialize handles each entry).
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkObjectReference hitterRef = default;
            if (!serializer.IsReader && Hitter != null && Hitter.TryGetComponent(out NetworkObject hitterNetObj))
                hitterRef = hitterNetObj;
            serializer.SerializeValue(ref hitterRef);
            if (serializer.IsReader)
                Hitter = hitterRef.TryGet(out NetworkObject readNetObj) ? readNetObj.gameObject : null;

            serializer.SerializeValue(ref DamageInfo);

            int additionalCount = !serializer.IsReader ? (AdditionalDamages?.Count ?? 0) : 0;
            serializer.SerializeValue(ref additionalCount);
            if (serializer.IsReader) AdditionalDamages = new List<DamageInfo>(additionalCount);
            for (int i = 0; i < additionalCount; i++)
            {
                DamageInfo entry = !serializer.IsReader ? AdditionalDamages[i] : default;
                serializer.SerializeValue(ref entry);
                if (serializer.IsReader) AdditionalDamages.Add(entry);
            }

            serializer.SerializeValue(ref HitDirection);
            serializer.SerializeValue(ref KnockbackForce);
            serializer.SerializeValue(ref KnockbackDuration);

            // Same asset-by-id trip DamageType makes just above, resolved through CombatManager instead.
            string bodyPartId = !serializer.IsReader ? (HitBodyPart != null ? HitBodyPart.GetId() : string.Empty) : string.Empty;
            serializer.SerializeValue(ref bodyPartId);
            if (serializer.IsReader)
                HitBodyPart = string.IsNullOrEmpty(bodyPartId) ? null : CombatManager.GetBodyPart(bodyPartId);
        }
#endif
    }
}