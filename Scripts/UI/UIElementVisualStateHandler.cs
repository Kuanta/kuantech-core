using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    public class UIElementVisualStateHandler : MonoBehaviour
    {
        public List<GameObject> Visuals;
        
        public void SetState(int visualState)
        {
            if (Visuals.IsNullOrEmpty()) return;
            if(!Visuals.IsValidIndex(visualState))
            {
                visualState = 0;
            }
            for (int i = 0; i < Visuals.Count; i++)
            {
                if (i == visualState)
                {
                    Visuals[i]?.SetActive(true);
                }
                else
                {
                    Visuals[i]?.SetActive(false);
                }
            }
        }
    }
}