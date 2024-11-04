using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoard : MonoBehaviour
    {
        public List<JamBoardSpot> Spots;

        [SerializeField] private JamBoardSpot SpotPrefab;
        [SerializeField] private EvenLayoutPlacer LayoutPlacer;
        
        public void Initialize()
        {
            foreach (var spot in Spots)
            {
                spot.ParentBoard = this;
                spot.PathNodeComponent.Initialize();
            }
        }

        public void BuildDock(int dockSize)
        {
            List<GameObject> spotObjects = new List<GameObject>();
            Spots = new List<JamBoardSpot>();
            for (int i = 0; i < dockSize; ++i)
            {
                JamBoardSpot waitingSpot = Instantiate(SpotPrefab);
                Spots.Add(waitingSpot);
                spotObjects.Add(waitingSpot.gameObject);
            }
            LayoutPlacer.SetChildren(spotObjects);
            LayoutPlacer.DistributeChilds();
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
        
        public virtual void Reset()
        {
            foreach (var spot in Spots)
            {
                spot.Reset();
            }
        }
    }
}