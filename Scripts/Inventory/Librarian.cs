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
    // public class ItemTemplate
    // {
    //     public int id;
    //     public string name;
    //     public int prefabId;
    //     public bool inPlace;
    // }

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

        [Header("Modifiers List")] 
        public ModifierDataDictionary ModifierDataDictionary;
        
        //public const string itemTemplatesPath = "/itemTemplates.yaml";
        public const string weaponsDataPath = "/weapons.yaml";
        public const string armorsDataPath = "/armors.yaml";
        //public Dictionary<int, ItemTemplate> itemTemplates = new Dictionary<int, ItemTemplate>();
        public Dictionary<int, ItemData> ItemDatas = new Dictionary<int, ItemData>();
        
        public void Awake()
        {
            Parse();
        }

        public void Parse()
        {
            //itemTemplates.Clear();
            ItemDatas.Clear();
            // Read Templates
            // var deserializer = new DeserializerBuilder()
            //     .WithNamingConvention(CamelCaseNamingConvention.Instance)
            //     .WithTagMapping("!ItemTemplate", typeof(ItemTemplate))
            //     .Build();
            
            //string yml = File.ReadAllText(Application.streamingAssetsPath + itemTemplatesPath);

            // List<ItemTemplate> templates = deserializer.Deserialize<List<ItemTemplate>>(yml);
            //
            // foreach (ItemTemplate template in templates)
            // {
            //     itemTemplates.Add(template.id, template);
            // }
            
            // Read Weapons
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!Weapon", typeof(WeaponData))
                .Build();
            
            string yml = File.ReadAllText(Application.streamingAssetsPath + weaponsDataPath);
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
        }

        #region Items

        public Item GetItemFromStateData(ItemStateData stateData)
        {
            ItemData itemData = ItemDatas[stateData.ItemId];
            Item item = Item.GetItemFromData(itemData);
            item.StateData = stateData;
            return item;
        }
        
        // /// <summary>
        // /// Returns item model prefab from template id
        // /// </summary>
        // /// <param name="itemId"></param>
        // /// <returns></returns>
        // public GameObject GetItemPrefab(int itemId)
        // {
        //     return TemplatePrefabs[ItemDatas[itemId].TemplateData.prefabId].ItemPrefab;
        // }
        
        /// <summary>
        /// Returns item drop model prefab from template id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public GameObject GetItemDropPrefab(int itemId)
        {
            try
            {
                return TemplatePrefabs[ItemDatas[itemId].Template.prefabId].ItemDropPrefab;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Returns game object of the item model
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public GameObject GetItemObject(int itemId)
        {
            GameObject prefab = GetItemTemplatePrefab(itemId).ItemPrefab;
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

        // public Sprite GetIconFromItemId(int itemId)
        // {
        //     return GetItemTemplatePrefab(itemId).ItemIcon;
        // }
        
        /// <summary>
        /// Returns item template visuals from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ItemTemplatePrefab GetItemTemplatePrefab(int itemId)
        {
            if (ItemDatas[itemId].Template.inPlace)
            {
                Debug.LogError("Trying to get a prefab that is labeled as in-place");
            }
            return TemplatePrefabs[ItemDatas[itemId].Template.prefabId];
        }
        #endregion
    }
}