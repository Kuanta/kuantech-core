using UnityEngine;
namespace Kuantech.Puzzle
{
    public class GridBoardBackground : MonoBehaviour {
        [SerializeField] private GameObject EvenBackground;
        [SerializeField] private GameObject OddBackground;

        public void SetBackground(int row, int col)
        {
            bool isEven = (row + col) % 2 == 0;
            EvenBackground.SetActive(isEven);
            OddBackground.SetActive(!isEven);
        }    
    }
}