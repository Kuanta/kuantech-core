using Kuantech.AI.Pathfinding;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    public class JamBoardPathNode : PathNode
    {
        public JamBoardSpot ParentSpot;

        public JamBoardPathNode(JamBoardSpot parentSpot)
        {
            ParentSpot = parentSpot;
        }
        public override bool IsPassable()
        {
            return !ParentSpot.IsSpotOccupied();
        }
        
        public override Vector3 GetPosition()
        {
            return ParentSpot.transform.position;
        }

        public override Quaternion GetRotation()
        {
            return ParentSpot.transform.rotation;
        }
    }
}