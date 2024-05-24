using Kuantech.Core;
using SupersonicWisdomSDK;

namespace Kuantech.GridlessSort
{
    public class AnimalSorterGameManager : GameManager
    {
        protected override void Awake()
        {
            base.Awake();
            // Subscribe
            SupersonicWisdom.Api.AddOnReadyListener(OnSupersonicWisdomReady);
            // Then initialize
            SupersonicWisdom.Api.Initialize();
        }

        protected override async void Start()
        {
            //DO nothing
        }
        private void OnSupersonicWisdomReady()
        {
            StartGame();
        }
    }
}