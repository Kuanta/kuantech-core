using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "StatAttribute", menuName = "Kuantech/Stats/StatAttribute")]
    public class StatAttribute : ScriptableObject {
        public string Id;
        public string Name;
    }
}