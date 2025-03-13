using System;
using System.Collections.Generic;
using System.IO;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Data;
using Kuantech.Rpg.Inventory;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kuantech.Rpg
{
    public enum ItemRarities
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }


    public class Librarian : Singleton<Librarian>
    {
        [Header("Item Prefabs")]
        //public List<ItemTemplatePrefab> templatePrefabs = new List<ItemTemplatePrefab>();
        public GameObject DefaultDropModel;
        
        [Header("Projectile Prefabs")]
        public List<GameObject> projectilePrefabs = new List<GameObject>();

        [Header("Skills")] 
        public DamageDealer DamageDealerPrefab;

        //public const string itemTemplatesPath = "/itemTemplates.yaml";
        public const string weaponsDataPath = "/weapons.yaml";
        public const string armorsDataPath = "/armors.yaml";
        
        
        //Dictionaries
        public List<ItemData> ItemDatasList;
        public Dictionary<string, ItemData> ItemDatas = new Dictionary<string, ItemData>();
        public List<ItemTemplate> ItemTemplatesList = new List<ItemTemplate>();
        public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, ItemTemplate>();

        public void Initialize()
        {
            ItemDatas.Clear();
            
            // Read Weapons
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!Weapon", typeof(WeaponData))
                .IgnoreUnmatchedProperties()
                .Build();

            string yml = BetterStreamingAssets.ReadAllText(Path.Combine(Application.streamingAssetsPath, weaponsDataPath));
            List<WeaponData> weapons = deserializer.Deserialize<List<WeaponData>>(yml);
            foreach (WeaponData data in weapons)
            {
                data.ItemType = ItemType.Weapon;
                ItemDatas.Add(data.Id, data);
            }
       
            
            // Read Armors
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!Armor", typeof(ArmorData))
                .Build();
            
            yml = BetterStreamingAssets.ReadAllText( Path.Combine(Application.streamingAssetsPath, armorsDataPath));
            List<ArmorData> armors = deserializer.Deserialize<List<ArmorData>>(yml);
            foreach (ArmorData data in armors)
            {
                data.ItemType = ItemType.Armor;
                ItemDatas.Add(data.Id, data);
            }
        }

        #region Items
        /// <summary>
        /// Returns item template
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ItemTemplate GetItemTemplate(string itemId)
        {
            try
            {
                ItemData itemData = ItemDatas[itemId];
                string templateId = itemData.ItemTemplateId;
                return GetItemTemplate(templateId);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        public Item GetItemFromStateData(ItemStateData stateData)
        {
            ItemData itemData = ItemDatas[stateData.ItemId];
            Item item = Item.GetItemFromData(itemData);
            item.StateData = stateData;
            return item;
        }
        
        /// <summary>
        /// Returns item drop model prefab from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public GameObject GetItemDropPrefab(string itemId)
        {
            try
            {
                ItemData itemData = ItemDatas[itemId];
                string templateId = itemData.ItemTemplateId;
                ItemTemplate template = GetItemTemplate(templateId);
                return template.ItemVisualPrefab.gameObject;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Returns the visual prefab for item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public GameObject GetItemVisualPrefab(string itemId)
        {
            ItemTemplate itemTemplate = GetItemTemplate(itemId);
            if (itemTemplate == null) return null;
            return itemTemplate.ItemVisualPrefab;
        }

        
        /// <summary>
        /// Returns drop model for given template. If the drop model for the item is null, returns the default drop model
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameObject GetItemDropObject(string itemId)
        {
            GameObject prefab = GetItemDropPrefab(itemId);
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


        public Sprite GetItemIcon(string itemId)
        {
            ItemTemplate itemTemplate = GetItemTemplate(itemId);
            if (itemTemplate == null) return null;
            return itemTemplate.ItemIcon;
        }
        #endregion
    }
}