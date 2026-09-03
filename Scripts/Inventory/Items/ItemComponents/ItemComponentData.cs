using System;
using static Kuantech.Core.Database.DataTable;

namespace Kuantech.Inventory
{
    [Serializable]
    public abstract class ItemComponentData
    {
        public abstract ItemComponent CreateInstance();

        public virtual void FillFromRowData(KtRowData rowData)
        {
            
        }
    }
}
