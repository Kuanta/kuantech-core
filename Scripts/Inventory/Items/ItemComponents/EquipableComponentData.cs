using System;
using System.Collections.Generic;
using Kuantech.Core.Database;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipableComponentData : ItemComponentData
    {
        public List<EquipmentSlotType> SuitableSlots;
        public List<EquipmentSlotType> OccupiedSlots;

        public override ItemComponent CreateInstance() => new EquipableComponent(this);

        public override void FillFromRowData(DataTable.KtRowData rowData)
        {
            base.FillFromRowData(rowData);
            SuitableSlots = ParseSlotIds(rowData.GetStringValue("SuitableSlots", ""));
            OccupiedSlots = ParseSlotIds(rowData.GetStringValue("OccupiedSlots", ""));
        }

        private static List<EquipmentSlotType> ParseSlotIds(string idsCsv)
        {
            var result = new List<EquipmentSlotType>();
            if (string.IsNullOrEmpty(idsCsv)) return result;

            foreach (var id in idsCsv.Split(','))
            {
                EquipmentSlotType slot = ItemsLibrary.GetEquipmentSlotById(id.Trim());
                if (slot != null) result.Add(slot);
            }
            return result;
        }
    }
}
