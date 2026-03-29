using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Inventory;
using UnityEngine;


namespace Kuantech.Rpg
{
    public class Librarian : SubManager
    {
        
        //
        // public List<ItemTemplate> ItemTemplateas;
        // private Dictionary<string, ItemTemplate> _itemTemplatesDict;

        // [Header("Projectile Prefabs")] 
        // public List<Projectile> Projectiles;
        // private Dictionary<string, Projectile> _projectilesDict;

        [Header("Items")] 
        public List<ItemData> Items; //Temporary
        public ItemsVault ItemsVault;

        
        public async override UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            if(ItemsVault != null) await ItemsVault.LoadDataFromList(Items);
        }

        #region Item Queries
        
        /// <summary>
        /// Returns item data from item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static ItemData GetItemData(string itemId)
        {
            Librarian librarian = GetContext<Librarian>();
            if (librarian.ItemsVault == null) return null;
            try
            {
                ItemData itemData = librarian.ItemsVault.GetDataById(itemId);
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
            return librarian.ItemsVault.GetItemTemplate(itemId);
        }
        
        public Item GetItemFromStateData(ItemStateData stateData)
        {
            ItemData itemData = ItemsVault.GetDataById(stateData.ItemId);
            Item item = Item.GetItemFromData(itemData);
            item.StateData = stateData;
            return item;
        }
        

        
        // /// <summary>
        // /// Returns the visual prefab for item
        // /// </summary>
        // /// <param name="itemId"></param>
        // /// <returns></returns>
        // public static GameObject GetItemVisualPrefab(string itemId)
        // {
        //     Librarian librarian = GetContext<Librarian>();
        //     ItemTemplate itemTemplate = GetItemTemplate(itemId);
        //     if (itemTemplate == null) return librarian.MissingPrefab;
        //     return itemTemplate.ItemVisualPrefab;
        // }
        //
        // /// <summary>
        // /// Returns prefab for projectile
        // /// </summary>
        // /// <param name="projectileId"></param>
        // /// <returns></returns>
        // public static Projectile GetProjectilePrefab(string projectileId)
        // {
        //     Librarian ctx = GetContext<Librarian>();
        //     if (ctx == null || !ctx._projectilesDict.ContainsKey(projectileId)) return null;
        //     return ctx._projectilesDict[projectileId];
        // }


        public static Sprite GetItemIcon(string itemId)
        {
            ItemTemplate itemTemplate = GetItemTemplate(itemId);
            if (itemTemplate == null) return null;
            return itemTemplate.ItemIcon;
        }
        #endregion
    }
}