using System;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoardElement : MonoBehaviour
    {
        [NonSerialized] public JamBoardSpot AssignedSpot;
        
        public void OnAssignedToSpot(JamBoardSpot spot)
        {
            AssignedSpot = spot;
        }

        public void RemoveFromSpot()
        {
            
        }
    }
}