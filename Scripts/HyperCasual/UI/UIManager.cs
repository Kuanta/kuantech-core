
namespace Kuantech.Core.HyperCasual
{
    public class UIManager : Singleton<UIManager>
    {
        
        //Panels
        public MainMenu MainMenu;
        public IngameMenu IngameMenu;
        public HeaderPanel HeaderPanel;
        
        public void Initialize()
        {
            ((HCGameManager)GameManager.Instance).StateChangeEvent += OnStateChange;
        }

        public void SetCurrencyAmount(int currencyType, int amount)
        {
            if (HeaderPanel == null) return;
            HeaderPanel.SetCurrencyAmount((Currencies)currencyType, amount);
        }
        
        private void OnStateChange(object sender, StateChangeData change)
        {
            IngameMenu.OnStateChange(change.NewState);
            MainMenu.OnStateChange(change.NewState);
            if (change.NewState == LevelState.Waiting)
            {
                IngameMenu.Close();
                MainMenu.Show();
            }
            else if(change.NewState == LevelState.Playing)
            {
                IngameMenu.Show();
                MainMenu.Close();
            }
        }
    }
}