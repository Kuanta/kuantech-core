using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoardSpot : MonoBehaviour
    {
        public JamBoard ParentBoard;
        private JamBoardPathNode PathNode;
        //public PathNodeComponent PathNodeComponent;
        private IJamBoardElement CurrentElement;
        private IJamBoardElement IncomingElement;

        public void Initialize(JamBoard parentBoard)
        {
            ParentBoard = parentBoard;
            PathNode = new JamBoardPathNode(this);
        }
        public IJamBoardElement GetOccupyingElement()
        {
            return CurrentElement;
        }

        public JamBoardPathNode GetPathNode()
        {
            if (PathNode == null)
            {
                PathNode = new JamBoardPathNode(this);
            }

            return PathNode;
        }
        public bool IsSpotOccupied()
        {
            return CurrentElement != null || IncomingElement != null;
        }

        public void ReserveSpot(IJamBoardElement incomingElement)
        {
            if (CurrentElement != null)
            {
                Debug.LogError("Trying to set incoming element while there is already an element");
            }

            IncomingElement = incomingElement;
            IncomingElement.SetAssignedSpot(this);
        }
        public void AssignElement(IJamBoardElement element)
        {
            if (IncomingElement != null && element != IncomingElement)
            {
                Debug.LogError("Big error");
            }

            IncomingElement = null;
            CurrentElement = element;
            CurrentElement.SetAssignedSpot(this);
        }

        public void ClearSpot()
        {
            CurrentElement = null;
            IncomingElement = null;
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
                CurrentElement.Despawn();
            }
            ClearSpot();
        }
    }
}