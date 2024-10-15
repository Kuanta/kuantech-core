using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoardSpot : MonoBehaviour
    {
        public JamBoard ParentBoard;
        public JamBoardElement CurrentElement;
        
        public bool IsSpotOccupied()
        {
            return CurrentElement != null;
        }

        public void AssignElement(JamBoardElement element)
        {
            CurrentElement = element;
            CurrentElement.OnAssignedToSpot(this);
        }

        public WorldPoint GetWorldPoint()
        {
            return new WorldPoint()
            {
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