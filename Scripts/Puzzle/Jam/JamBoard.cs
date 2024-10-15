using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoard : MonoBehaviour
    {
        public List<JamBoardSpot> Spots;

        public void Initialize()
        {
            foreach (var spot in Spots)
            {
                spot.ParentBoard = this;
            }
        }

        public bool IsBoardFull()
        {
            foreach (var spot in Spots)
            {
                if (!spot.IsSpotOccupied()) return false;
            }
            return true;
        }

        public int GetEmptySpotCount()
        {
            int emptySpots = 0;
            foreach (var spot in Spots)
            {
                if (spot.IsSpotOccupied())
                {
                    continue;
                }

                emptySpots++;
            }

            return emptySpots;
        }
        
        public JamBoardSpot GetAvailableSpot()
        {
            foreach (var spot in Spots)
            {
                if (spot.IsSpotOccupied())
                {
                    continue;
                }

                return spot;
            }

            return null;
        }

        public bool SlotElement(JamBoardElement element)
        {
            JamBoardSpot spot = GetAvailableSpot();
            if (spot == null) return false;
            spot.AssignElement(element);
            return true;
        }
        
        public void Reset()
        {
            foreach (var spot in Spots)
            {
                spot.Reset();
            }
        }
    }
}