using TMPro;
using UnityEngine;
namespace Kuantech.ArcadeIdle.UI
{
    public class OrderArriveTimeIndicator : MonoBehaviour {
        public ResourceShop ResourceShop = null;
        [SerializeField] private TMP_Text TimerText;

        private void Update()
        {
            if(ResourceShop == null) return;
            bool isOrderComing = ResourceShop.IsOrderIncoming();
            if(!isOrderComing)
            {
                TimerText.gameObject.SetActive(false);
                return;
            }
            TimerText.gameObject.SetActive(true);
            int remainingTime = (int)ResourceShop.GetRemainingTime();
            TimerText.text = ElapsedSecondsToTimerString(remainingTime);
        }

        private string ElapsedSecondsToTimerString(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}