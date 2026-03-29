using System;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    /// <summary>
    /// A librarian that handles items
    /// </summary>
    [Serializable]
    public abstract class ItemLibrarian
    {
        public virtual void Initialize(ItemsManager parentManager){}
        public abstract ItemData GetItemData(string itemId);
    }

    public class ItemsManager : SubManager
    {
        [SerializeReference]
        public ItemLibrarian Librarian;

        public async override UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);
        }

        public static ItemData GetItemData(string itemId)
        {
            var ctx = GetContext<ItemsManager>();
            if (ctx == null) return null;
            return ctx._GetItemData(itemId);
        }

        protected virtual ItemData _GetItemData(string itemId)
        {
            if(Librarian == null) return null;
            return Librarian.GetItemData(itemId);
        }
    }
}