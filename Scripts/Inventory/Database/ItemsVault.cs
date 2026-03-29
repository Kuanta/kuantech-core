using System.Collections.Generic;
using Kuantech.Core.Data;
using SQLite4Unity3d;

namespace Kuantech.Inventory
{
    [Table("items")]
    public class ItemDataEntry
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string Name { get; set; }
        public float Weight { get; set; }
        public bool Stackable { get; set; }
    }

    public class ItemsVault : Vault<ItemData>
    {
        //public List<ItemData> ItemDatasList;
        //public Dictionary<string, ItemData> ItemDatas = new Dictionary<string, ItemData>();
        public List<ItemTemplate> ItemTemplatesList = new List<ItemTemplate>();
        public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, ItemTemplate>();

        protected override void Initialize()
        {
            base.Initialize();
            ItemTemplates = new Dictionary<string, ItemTemplate>();
            foreach (var itemTemplate in ItemTemplatesList)
            {
                ItemTemplates[itemTemplate.TemplateId] = itemTemplate;
            }
        }
        
        public ItemTemplate GetItemTemplate(string itemId)
        {
            ItemData data = GetDataById(itemId);
            if (data == null) return null;
            string templateId = data.ItemTemplateId;
            return GetItemTemplateByTemplateId(templateId);
        }

        public ItemTemplate GetItemTemplateByTemplateId(string templateId)
        {
            if (ItemTemplates.ContainsKey(templateId)) return ItemTemplates[templateId];
            return null;
        }

    }
}