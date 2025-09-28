using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A rank indicator using sprites;
    /// </summary>
    public class IconRankIndicator : MonoBehaviour
    {
        [SerializeField] private List<Image> Icons;
        [SerializeField] private Color ActiveColor = Color.white;
        [SerializeField] private Color InactiveColor = Color.gray;

        public void SetRank(int rank)
        {
            for (int i = 0; i < Icons.Count; i++)
            {
                Icons[i].color = i >= rank ? ActiveColor : InactiveColor;
            }
        }
    }
}