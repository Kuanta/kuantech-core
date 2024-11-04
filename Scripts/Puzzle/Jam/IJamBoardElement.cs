using System;

namespace Kuantech.Puzzle.Jam
{
    public interface IJamBoardElement
    {
        public void SetAssignedSpot(JamBoardSpot spot)
        {
        }

        public void RemoveFromSpot()
        {
        }

        public void Despawn()
        {
            
        }
    }
}