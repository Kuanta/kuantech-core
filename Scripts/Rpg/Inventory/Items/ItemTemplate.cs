using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    [CreateAssetMenu(fileName = "Item Template", menuName = "Kuantech/Inventory/Item Template")]
    public class ItemTemplate : ScriptableObject
    {
        public string TemplateId;
        public GameObject ItemVisualPrefab;
        public GameObject ItemDropPrefab;
        public Sprite ItemIcon;
    }
}