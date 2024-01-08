using Kuantech.ArcadeIdle.UI;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class UIPanelOpenTrigger : MonoBehaviour {
        
        [SerializeField] private string PanelId;
        private bool _openedPanel = false;
        private void OnTriggerEnter(Collider collider)
        {
            if(collider.gameObject.TryGetComponent(out ArcadeIdlePlayer player))
            {
                _openedPanel = false;
                OpenPanel(player);
            }
        }

        private void OnTriggerStay(Collider collider)
        {
            if(_openedPanel) return;
            if (collider.gameObject.TryGetComponent(out ArcadeIdlePlayer player))
            {
                OpenPanel(player);
            }
        }

        private void OpenPanel(ArcadeIdlePlayer player)
        {
            if (!player.IsStandingStill()) return;
            ArcadeIdleUIManager.OpenPanel(PanelId);
            _openedPanel = true;
        }
    }
}