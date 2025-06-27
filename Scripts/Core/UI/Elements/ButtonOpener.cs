using UnityEngine;

namespace Kuantech.Core.UI
{
    public class ButtonOpener : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private UIMenu MenuToOpen;
        public void OnClick()
        {
            throw new System.NotImplementedException();
        }
    }
}