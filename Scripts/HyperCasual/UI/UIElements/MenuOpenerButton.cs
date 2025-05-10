using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class MenuOpenerButton : MonoBehaviour
    {
        [SerializeField] private Button Button;
        [SerializeField] private UIMenu MenuToOpen;
        private void Start()
        {
            Button.onClick.AddListener(MenuToOpen.Open);
        }
    }
}