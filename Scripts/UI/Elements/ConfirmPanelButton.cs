using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class ConfirmPanelButton : MonoBehaviour
    {
        [SerializeField] private ConfirmPanel ConfirmPanel;

        private void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(ConfirmPanel.Open);
        }

        public void SetConfirmAction(UnityAction confirmAction)
        {
            ConfirmPanel.OnConfirm = confirmAction;
        }
    }
}