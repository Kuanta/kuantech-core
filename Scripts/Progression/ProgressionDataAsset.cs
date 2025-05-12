using UnityEngine;

namespace Kuantech.Progression
{
    [CreateAssetMenu(fileName = "ProgressionAsset", menuName = "Kuantech/Progression/ProgressionAsset")]
    public class ProgressionDataAsset : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Icon;
    }
}