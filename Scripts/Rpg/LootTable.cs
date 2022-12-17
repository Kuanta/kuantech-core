using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Inventory.Items;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech
{
    public enum LootType
    {
        None,
        Coin,
        Potion,
        Item,
    }
    
    [Serializable]
    public struct DropChance
    {
        public LootType LootType;
        public float Chance;
        public int ItemId;
        public List<float> RarirtyChances; //Chance to receive an item 
    }
    
    public class LootTable : MonoBehaviour
    {
        public int Experience = 1;
        public int[] GoldDropInterval = new int[2]
        {
            5, 25,
        };
        public float[] RegenDropInterval = new float[2]
        {
            0.25f, 0.75f,
        };
        public List<DropChance> DropChances;
        public List<DropChance> ItemDrops;

        private void Awake()
        {
            DropChances = DropChances.OrderBy(x => x.Chance).ToList();
            ItemDrops = ItemDrops.OrderBy(x => x.Chance).ToList();
        }

        public void OnDeath()
        {
            //Add Experience
            GameManager.Instance.Player.Stats.EarnExperience(Experience);
            
            if (DropChances.IsNullOrEmpty()) return;
            //Decide what to drop

            LootType lootType = DropChances[GetLoot(DropChances)].LootType;
            
            switch (lootType)
            {
                case LootType.None:
                    return;
                case LootType.Coin:
                    AddCoin();
                    break;
                case LootType.Potion:
                    AddPotion();
                    break;
                case LootType.Item:
                    AddItem();
                    break;
            }
        }
        
        /// <summary>
        /// Gets a loot from the drop chance list
        /// </summary>
        /// <param name="drops">List of drop chances</param>
        /// <returns></returns>
        private static int GetLoot(List<DropChance> drops)
        {
            float upperlimit = drops.Sum(x => x.Chance);
            float chance = Random.Range(0f, upperlimit);
            float lowLimit = 0f;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (chance >= lowLimit && chance < drops[i].Chance + lowLimit)
                {
                    //Thats it
                    return i;
                }
                lowLimit = drops[i].Chance;
            }
            return 0;
        }
        
        /// <summary>
        /// Gets the rarity from a list of rarity chances. First element should be the lowest rarity, 
        /// </summary>
        /// <param name="rarityChances"></param>
        /// <returns></returns>
        [Button("Test Rarity")]
        private static ItemRarities GetItemRarity(List<float> rarityChances)
        {
            return ItemRarities.Legendary;
            if (rarityChances.Count != Enum.GetNames(typeof(ItemRarities)).Length)
            {
                Debug.LogWarning("Rarity chances isn't on the same size of rarity enum");
                return 0; //Return common. Developers that don't know their codes don't deserve any better.
            }
            float totalChances = rarityChances.Sum();
            float random = Random.Range(0f, totalChances);
            for (int i = rarityChances.Count - 1; i >= 0; --i)
            {
                if (random >= totalChances - rarityChances[i])
                {
                    return (ItemRarities) i;
                }

                totalChances -= rarityChances[i];
            }

            return ItemRarities.Common;
        }
        
        private void AddCoin()
        {
            int coinAmount = Random.Range(GoldDropInterval[0], GoldDropInterval[1]);
            if (GameManager.Instance.CurrentLevel == null) return;
            GameManager.Instance.CurrentLevel.AddCoin(coinAmount);
        }

        private void AddPotion()
        {
            float potChance = Random.Range(0f, 1f);
            float regenPercentage = Random.Range(RegenDropInterval[0], RegenDropInterval[1]);
            if (potChance <= 0.5f)
            {
                //Health pot todo: Play potion effects
                GameManager.Instance.Player.ReceivePercentageHeal(regenPercentage);
            }
            else
            {
                //Energy pot todo: Play potion effects
                GameManager.Instance.Player.ReceivePercentageEnergy(regenPercentage);
            }
        }

        private void ThrowItem(int templateId)
        {
            //Get item object
            GameObject itemObject = Librarian.Instance.GetItemDropObject(templateId);
            if (itemObject == null)
            {
                return;
            }
            //Register the object to current Level
            if (GameManager.Instance.CurrentLevel != null)
            {
                GameManager.Instance.CurrentLevel.AddSpawnedObject(itemObject);
            }
            
            itemObject.transform.position = transform.position + Vector3.up * 0.1f;
            ItemDropModel dropModel = itemObject.GetComponent<ItemDropModel>();
            if (dropModel == null)
            {
                dropModel = itemObject.AddComponent<ItemDropModel>();
                Kuantech.Physics.Rigidbody rb = itemObject.AddComponent<Kuantech.Physics.Rigidbody>();
                dropModel.Rigidbody = rb;
            }
            Vector3 impulseDirection = transform.position - GameManager.Instance.Player.transform.position;
            impulseDirection.y = 0f;
            impulseDirection.Normalize();
            float trajectoryTime = dropModel.SetTrajectory(2f, 10f, new Vector2(impulseDirection.x, impulseDirection.z), 9f);
            dropModel.Rigidbody.TimeScale = 2f; //Speed up the trajectory
            dropModel.GoToTarget(GameManager.Instance.Player.transform, Vector3.up, trajectoryTime / 2f, drop =>
            {
                drop.gameObject.SetActive(false);
            });
        }
        
        /// <summary>
        /// Adds
        /// </summary>
        private void AddItem()
        {
            int dropId = GetLoot(ItemDrops);
            int itemId = ItemDrops[dropId].ItemId;
            Item item = Item.GetItemFromData(Librarian.Instance.ItemDatas[itemId]);
            
            //Get rarity
            ItemRarities rarity;
            if (ItemDrops[dropId].RarirtyChances.IsNullOrEmpty())
            {
                rarity = ItemRarities.Legendary;
            }
            else
            {
                rarity = GetItemRarity(ItemDrops[dropId].RarirtyChances);
            }
            item.SetItemRarity(rarity);
            ThrowItem(item.templateData.id);
            
            //Items are staged until level complete
            GameManager.Instance.CurrentLevel.AddItem(item);
        }
    }
}