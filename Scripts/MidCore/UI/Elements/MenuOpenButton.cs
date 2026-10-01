using Kuantech.Midcore;
using Kuantech.Rpg;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class MenuOpenButton : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private MenuGroup MenuGroup;
        [SerializeField] private UIMenu MenuToOpen;
        [SerializeField] private string MenuIDToOpen;
        [SerializeField] private TMP_Text LevelRequirementText;
        
        [Header("Menu States")] 
        [SerializeField] private GameObject OpenedStateVisual;
        [SerializeField] private GameObject ClosedStateVisual;
        [SerializeField] private GameObject UnlockedContent;
        [SerializeField] private GameObject LockedStateVisual;
        [SerializeField] private Animator Animator;
        private static readonly int Opened = Animator.StringToHash("Opened");

        [SerializeField] private int PlayerLevelRequirement = 0;
        
        public void Initialize()
        {
            if (MenuGroup != null)
            {
                MenuGroup.OnMenuOpened += OnMenuOpened;
            }

            UIMenu menu = MenuToOpen;
            if (menu == null && MenuIDToOpen != null)
            {
                menu = UIManager.GetMenuById(MenuIDToOpen);
            }

            if (menu != null)
            {
                menu.OnMenuOpened += SetOpenedVisual;
                menu.OnMenuClosed += SetClosedVisual;
            }

            if (menu.IsVisible())
            {
                SetOpenedVisual();
            }
            else
            {
                SetClosedVisual();
            }

            if (LevelRequirementText != null)
            {
                LevelRequirementText.text = $"Level {PlayerLevelRequirement+1}";
            }
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            SetLockedState(!IsUnlocked());
        }
        
        private bool IsUnlocked()
        {
            LevelVariable playerLevelVariable = ProgressionManager.GetPlayerLevel();
            int playerLevel = playerLevelVariable?.CurrentLevel ?? 0;
            if (PlayerLevelRequirement > playerLevel) return false;
            return true;
        }
        
        public void OnClick()
        {
            if (!IsUnlocked()) return;
            if (MenuToOpen != null)
            {
                if (MenuGroup != null)
                {
                    MenuGroup.OpenMenu(MenuToOpen);
                }
                else
                {
                    UIManager.OpenMenu(MenuToOpen);
                }
            }else if (!MenuIDToOpen.IsNullOrEmpty())
            {
                if (MenuGroup != null)
                {
                    MenuGroup.OpenMenu(MenuIDToOpen);
                }
                else
                {
                    UIManager.OpenMenu(MenuIDToOpen);
                }
            }
            else
            {
                Debug.LogWarning("Menu to open is not set in MenuOpenButton. Please set it in the inspector or use MenuIDToOpen.");
            }
        }

        private void OnMenuOpened(UIMenu menu)
        {
            bool isOpened = MenuToOpen == menu || menu.MenuId.Equals(MenuIDToOpen);
            if (isOpened)
            {
                SetOpenedVisual();
            }
            else
            {
                SetClosedVisual();
            }
            UpdateVisual();
        }

        private void SetLockedState(bool locked)
        {
            if(LockedStateVisual != null) LockedStateVisual.SetActive(locked);
            if(UnlockedContent != null) UnlockedContent.SetActive(!locked);
        }
        
        private void SetOpenedVisual()
        {
            if(OpenedStateVisual != null) OpenedStateVisual.SetActive(true);
            if(ClosedStateVisual != null) ClosedStateVisual.SetActive(false);
            if(Animator != null) Animator.SetBool(Opened, true);
        }

        private void SetClosedVisual()
        {
            if(OpenedStateVisual != null) OpenedStateVisual.SetActive(false);
            if(ClosedStateVisual != null) ClosedStateVisual.SetActive(true);
            if(Animator != null) Animator.SetBool(Opened, false);
        }
    }
}