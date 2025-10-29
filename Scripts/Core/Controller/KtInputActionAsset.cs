using UnityEngine;

namespace Kuantech.Core.Controller
{
    [CreateAssetMenu(fileName = "KtInputActionAsset", menuName = "Kuantech/Input/KtInputActionAsset", order = 1)]
    public class KtInputActionAsset : ScriptableObject
    {
        public string ActionId;
    }
}