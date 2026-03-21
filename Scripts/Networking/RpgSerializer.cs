using FishNet.Object;
using FishNet.Serializing;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Rpg.Managers;
using UnityEngine;

public static class RpgSerializer
{
    public static void WriteAttributeDefinition(this Writer writer, AttributeDefinition value)
    {
        writer.WriteString(value.AttributeAsset != null ? value.AttributeAsset.Id : "");
        writer.WriteSingle(value.BaseValue);
        writer.WriteSingle(value.ValuePerRank);
        writer.WriteSingle(value.ValuePerLevel);
    }

    public static AttributeDefinition ReadAttributeDefinition(this Reader reader)
    {
        string assetId = reader.ReadString();
        return new AttributeDefinition
        {
            AttributeAsset = RpgManager.GetAttributeAssetById(assetId),
            BaseValue = reader.ReadSingle(),
            ValuePerRank = reader.ReadSingle(),
            ValuePerLevel = reader.ReadSingle(),
        };
    }

    public static void WriteStatModifier(this Writer writer, StatModifier value)
    {
        writer.WriteString(value.AttributeAsset != null ? value.AttributeAsset.Id : "");
        writer.WriteString(value.ModifierTag ?? "");
        writer.WriteSingle(value.BaseValue);
        writer.WriteSingle(value.LevelToValueFactor);
        writer.WriteInt32(value.Level);
        writer.WriteInt32((int)value.ModifierType);
    }

    public static void WriteResourceAsset(this Writer writer, ResourceAsset value)
    {
        writer.WriteString(value != null ? value.Id : "");
    }

    public static ResourceAsset ReadResourceAsset(this Reader reader)
    {
        return RpgManager.GetResourceAssetById(reader.ReadString());
    }

    public static StatModifier ReadStatModifier(this Reader reader)
    {
        string assetId = reader.ReadString();
        return new StatModifier
        {
            AttributeAsset = RpgManager.GetAttributeAssetById(assetId),
            ModifierTag = reader.ReadString(),
            BaseValue = reader.ReadSingle(),
            LevelToValueFactor = reader.ReadSingle(),
            Level = reader.ReadInt32(),
            ModifierType = (ModifierTypes)reader.ReadInt32(),
        };
    }

    public static void WriteDamageType(this Writer writer, DamageType value)
    {
        writer.WriteString(value != null ? value.GetId() : "");
    }

    public static DamageType ReadDamageType(this Reader reader)
    {
        return RpgManager.GetDamageTypeById(reader.ReadString());
    }

    public static void WriteDamageInfo(this Writer writer, DamageInfo value)
    {
        writer.WriteDamageType(value.DamageType);
        writer.WriteSingle(value.DamageAmount);
        writer.WriteBoolean(value.IsCritical);
    }

    public static DamageInfo ReadDamageInfo(this Reader reader)
    {
        return new DamageInfo
        {
            DamageType = reader.ReadDamageType(),
            DamageAmount = reader.ReadSingle(),
            IsCritical = reader.ReadBoolean(),
        };
    }

    public static void WriteHitInfo(this Writer writer, HitInfo value)
    {
        // Hitter: send as NetworkObject (null if not networked)
        NetworkObject hitterNob = value.Hitter != null
            ? value.Hitter.GetComponent<NetworkObject>()
            : null;
        writer.WriteNetworkObject(hitterNob);
        writer.WriteDamageInfo(value.DamageInfo);
        writer.WriteList(value.AdditionalDamages);
        writer.WriteVector3(value.HitDirection);
        writer.WriteSingle(value.KnockbackForce);
        writer.WriteSingle(value.KnockbackDuration);
    }

    public static HitInfo ReadHitInfo(this Reader reader)
    {
        NetworkObject hitterNob = reader.ReadNetworkObject();
        return new HitInfo
        {
            Hitter = hitterNob != null ? hitterNob.gameObject : null,
            DamageInfo = reader.ReadDamageInfo(),
            AdditionalDamages = reader.ReadList<DamageInfo>(),
            HitDirection = reader.ReadVector3(),
            KnockbackForce = reader.ReadSingle(),
            KnockbackDuration = reader.ReadSingle(),
        };
    }
}
