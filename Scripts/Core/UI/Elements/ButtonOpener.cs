using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonOpener : MonoBehaviour, IUIButtonAction
    {
        [SerializeField] private UIMenu MenuToOpen;
        public void OnClick()
        {
            throw new System.NotImplementedException();
        }
    }
}