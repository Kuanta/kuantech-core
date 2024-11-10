using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class ModularTileVisualPiece : MonoBehaviour
    {
        [Serializable]
        public struct DirectionalityConditionEntry
        {
            public GridBoard.Directions Direciton;
            public bool RequiredState;
        }

        public List<DirectionalityConditionEntry> Requirements;
        
        /// <summary>
        /// Toggles the visual depending on the connectivity
        /// </summary>
        /// <param name="eightConnectivitiy"></param>
        public void Toggle(bool[] eightConnectivitiy)
        {
            foreach (var req in Requirements)
            {
                bool connectivityAtDirection = eightConnectivitiy[(int)req.Direciton];
                if (connectivityAtDirection != req.RequiredState)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }
            gameObject.SetActive(true);
        }
    }
}