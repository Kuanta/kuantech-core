using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoardSpot : MonoBehaviour
    {
        public JamBoard ParentBoard;
        private JamBoardElement CurrentElement;
        private JamBoardElement IncomingElement;

        public JamBoardElement GetOccupyingElement()
        {
            return CurrentElement;
        }
        
        public bool IsSpotOccupied()
        {
            return CurrentElement != null || IncomingElement != null;
        }

        public void ReserveSpot(JamBoardElement incomingElement)
        {
            if (CurrentElement != null)
            {
                Debug.LogError("Trying to set incoming element while there is already an element");
            }

            IncomingElement = incomingElement;
            IncomingElement.AssignedSpot = this;
        }
        public void AssignElement(JamBoardElement element)
        {
            if (IncomingElement != null && element != IncomingElement)
            {
                Debug.LogError("Big error");
            }

            IncomingElement = null;
            CurrentElement = element;
            CurrentElement.OnAssignedToSpot(this);
        }

        public WorldPoint GetWorldPoint()
        {
            return new WorldPoint()
            {
                Target = transform,
                Position = transform.position,
                Rotation = transform.rotation,
            };
        }
        public void Reset()
        {
            if (CurrentElement != null)
            {
                //Should we destroy it?
                Destroy(CurrentElement.gameObject);
            }
            CurrentElement = null;
        }
    }
}