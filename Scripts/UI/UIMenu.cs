using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class UIMenu : MonoBehaviour
    {
        public string MenuId;
        public float ShowDelay = 0f;
        [SerializeField] protected Button CloseButton;

        protected virtual void Start()
        {
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(Close);
            }
        }
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            gameObject.SetActive(false);
        }

    }
}