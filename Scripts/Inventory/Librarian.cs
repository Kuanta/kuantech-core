using System;
using System.Collections.Generic;
using System.IO;
using Kuantech.Core.Utils;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kuantech.Core
{
    public class ItemTemplate
    {
        public int id;
        public string name;
        public int prefabId;
        public bool inPlace;
    }

    public enum ItemRarities
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }
    
    [Serializable]
    public struct ItemTemplatePrefab
    {
        public GameObject ItemPrefab;
        public GameObject ItemDropPrefab;
        public Sprite ItemIcon;
    }

    [Serializable]
    public class TemplatePrefabDictionary : SerializableDictionary<int, ItemTemplatePrefab>{}

    public class Librarian : Singleton<Librarian>
    {
        [Header("Item Prefabs")]
        //public List<ItemTemplatePrefab> templatePrefabs = new List<ItemTemplatePrefab>();
        public TemplatePrefabDictionary TemplatePrefabs = new TemplatePrefabDictionary();
        public GameObject DefaultDropModel;
        
        [Header("Projectile Prefabs")]
        public List<GameObject> projectilePrefabs = new List<GameObject>();

        [Header("Skills")] 
        public DamageDealer DamageDealerPrefab;
        public List<AttackSkillData> AttackSkillDataList = new List<AttackSkillData>();

        [Header("Modifiers List")] 
        public ModifierDataDictionary ModifierDataDictionary;
        
        public const string itemTemplatesPath = "/itemTemplates.yaml";
        public const string weaponsDataPath = "/weapons.yaml";
        public const string armorsDataPath = "/armors.yaml";
        public Dictionary<int, ItemTemplate> itemTemplates = new Dictionary<int, ItemTemplate>();
        public Dictionary<int, ItemData> ItemDatas = new Dictionary<int, ItemData>();
        public Dictionary<int, AttackSkillData> AttackSkillDatas = new Dictionary<int, AttackSkillData>();
        
        public void Awake()
        {
            Parse();
        }

        public void Parse()
        {
            itemTemplates.Clear();
            ItemDatas.Clear();
            // Read Templates
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!ItemTemplate", typeof(ItemTemplate))
                .Build();
            
            string yml = File.ReadAllText(Application.streamingAssetsPath + itemTemplatesPath);

            List<ItemTemplate> templates = deserializer.Deserialize<List<ItemTemplate>>(yml);

            foreach (ItemTemplate template in templates)
            {
                itemTemplates.Add(template.id, template);
            }
            
            // Read Weapons
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!Weapon", typeof(WeaponData))
                .Build();
            
            yml = File.ReadAllText(Application.streamingAssetsPath + weaponsDataPath);
            List<WeaponData> weapons = deserializer.Deserialize<List<WeaponData>>(yml);
            foreach (WeaponData data in weapons)
            {
                data.ItemType = Enums.ItemType.Weapon;
                ItemDatas.Add(data.id, data);
            }
            
            // Read Armors
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!Armor", typeof(ArmorData))
                .Build();
            
            yml = File.ReadAllText(Application.streamingAssetsPath + armorsDataPath);
            List<ArmorData> armors = deserializer.Deserialize<List<ArmorData>>(yml);
            foreach (ArmorData data in armors)
            {
                data.ItemType = Enums.ItemType.Armor;
                ItemDatas.Add(data.id, data);
            }
            
            //Read Skills
            foreach (var data in AttackSkillDataList)
            {
                AttackSkillDatas[data.Id] = data;
            }
        }

        #region Items
        /// <summary>
        /// Returns item model prefab from template id
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public GameObject GetItemPrefab(int templateId)
        {
            return TemplatePrefabs[itemTemplates[templateId].prefabId].ItemPrefab;
        }
        
        /// <summary>
        /// Returns item drop model prefab from template id
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public GameObject GetItemDropPrefab(int templateId)
        {
            try
            {
                return TemplatePrefabs[itemTemplates[templateId].prefabId].ItemDropPrefab;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Returns game object of the item model
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameObject GetItemObject(int index)
        {
            GameObject prefab = GetItemPrefab(index);
            if (prefab == null) return null;
            return GameManager.Instance.Pool.GetObject(prefab);
        }
        
        /// <summary>
        /// Returns drop model for given template. If the drop model for the item is null, returns the default drop model
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameObject GetItemDropObject(int index)
        {
            GameObject prefab = GetItemDropPrefab(index);
            if (prefab == null && DefaultDropModel == null)
            {
                return null;
            }
            if (prefab == null && DefaultDropModel != null)
            {
                return GameManager.Instance.Pool.GetObject(DefaultDropModel);
            }
            return GameManager.Instance.Pool.GetObject(prefab);
        }
        
        public GameObject GetProjectilePrefab(int index)
        {
            return index == -1 ? null : projectilePrefabs[index];
        }

        public Sprite GetIconFromItemId(int itemId)
        {
            return GetItemTemplatePrefab(itemId).ItemIcon;
        }
        
        /// <summary>
        /// Returns item template visuals from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ItemTemplatePrefab GetItemTemplatePrefab(int itemId)
        {
            int tempalteId = ItemDatas[itemId].templateId;
            return TemplatePrefabs[itemTemplates[tempalteId].prefabId];
        }
        #endregion
        

        #region Skills
        /// <summary>
        /// Instantiates the corresponding skill given the id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AttackSkill GetAttackSkill(int id)
        {
            switch (id)
            {
                case 0:
                    return new Slam(AttackSkillDatas[id]);
                case 1:
                    return new QuickStrike(AttackSkillDatas[id]);
                case 2:
                    return new ArcaneBlast(AttackSkillDatas[id]);
                case 3:
                    return new ArrowBarrage(AttackSkillDatas[id]);
                case 4:
                    return new ArcaneArrow(AttackSkillDatas[id]);
                case 5:
                    return new PowerShot(AttackSkillDatas[id]);
                case 6:
                    return new GhostWeapon(AttackSkillDatas[id]);
                case 7:
                    return new MagicOrb(AttackSkillDatas[id]);
                case 8:
                    return new ArcaneEchoe(AttackSkillDatas[id]);
                default:
                    return null;
            }
        }

        #endregion
    }
}