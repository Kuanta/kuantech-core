using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridTileActorModule : ActorModule
    {
        [SerializeField] private BoardTile BoardTile;
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Despawned && BoardTile != null && BoardTile.ParentBoard != null)
            {
                BoardTile.ParentBoard.UnsetTile(BoardTile);
            }
        }
    }
}