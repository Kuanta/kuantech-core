using UnityEngine;

namespace Kuantech.TowerDefense
{
    [CreateAssetMenu(fileName = "EnemyTag", menuName = "Kuantech/TowerDefense/EnemyTag")]
    public class EnemyTagAsset : ScriptableObject
    {
        public string TagId;
        public Color DisplayColor = Color.white;
    }
}