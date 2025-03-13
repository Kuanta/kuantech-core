using System;
using System.Collections.Generic;
using System.IO;
using Kuantech.Core;
using Kuantech.Core.Utils;
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


    public class Librarian : SubManager
    {
        [Header("Prefabs")] 
        public GameObject MissingPrefab;
        
        public List<ItemTemplate> ItemTemplateas;
        private Dictionary<string, ItemTemplate> _itemTemplatesDict;

        [Header("Projectile Prefabs")] 
        public List<Projectile> Projectiles;
        private Dictionary<string, Projectile> _projectilesDict;

        [Header("Skills")] 
        public DamageDealer DamageDealerPrefab;
        
        [Header("Databases")]
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

        #region Queries
        
        /// <summary>
        /// Returns item data from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static ItemData GetItemData(string itemId)
        {
            Librarian librarian = GetContext<Librarian>();
            try
            {
                ItemData itemData = librarian.ItemDatas[itemId];
                return itemData;

            }
            catch (Exception e)
            {
                return null;
            }
        }
        /// <summary>
        /// Returns item template from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static ItemTemplate GetItemTemplate(string itemId)
        {
            ItemData data = GetItemData(itemId);
            if (data == null) return null;
            Librarian librarian = GetContext<Librarian>();
            if(librarian.ItemTemplates.ContainsKey(data.ItemTemplateId))
            {
                return librarian.ItemTemplates[data.ItemTemplateId];
            }
            return null;
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
        public static GameObject GetItemDropPrefab(string itemId)
        {
            Librarian librarian = GetContext<Librarian>();
            if (librarian == null)
            {
                return librarian.MissingPrefab;
            }
            try
            {
                ItemData itemData = librarian.ItemDatas[itemId];
                if (itemData == null)
                {
                    return librarian.MissingPrefab;
                }
                string templateId = itemData.ItemTemplateId;
                ItemTemplate template = GetItemTemplate(templateId);
                if (template == null)
                {
                    return librarian.MissingPrefab;
                }
                if (template.ItemDropPrefab != null)
                {
                    return template.ItemDropPrefab.gameObject;

                }if (template.ItemVisualPrefab != null)
                {
                    return template.ItemVisualPrefab.gameObject;
                }

                return librarian.MissingPrefab;
            }
            catch (Exception e)
            {
                return librarian.MissingPrefab;
            }
        }
        
        /// <summary>
        /// Returns the visual prefab for item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static GameObject GetItemVisualPrefab(string itemId)
        {
            Librarian librarian = GetContext<Librarian>();
            ItemTemplate itemTemplate = GetItemTemplate(itemId);
            if (itemTemplate == null) return librarian.MissingPrefab;
            return itemTemplate.ItemVisualPrefab;
        }
        
        /// <summary>
        /// Returns prefab for projectile
        /// </summary>
        /// <param name="projectileId"></param>
        /// <returns></returns>
        public static Projectile GetProjectilePrefab(string projectileId)
        {
            Librarian ctx = GetContext<Librarian>();
            if (ctx == null || !ctx._projectilesDict.ContainsKey(projectileId)) return null;
            return ctx._projectilesDict[projectileId];
        }


        public static Sprite GetItemIcon(string itemId)
        {
            ItemTemplate itemTemplate = GetItemTemplate(itemId);
            if (itemTemplate == null) return null;
            return itemTemplate.ItemIcon;
        }
        #endregion
    }
}