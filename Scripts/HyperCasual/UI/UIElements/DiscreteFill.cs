using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class DiscreteFill : MonoBehaviour
    {
        [SerializeField] public List<GameObject> FillChunks;

        public void SetFill(int fillAmount)
        {
            fillAmount = Mathf.Clamp(fillAmount, 0, FillChunks.Count);
            for (int i = 1; i <= FillChunks.Count; ++i)
            {
                FillChunks[i - 1].gameObject.SetActive(i <= fillAmount);
            }
        }
    }
}