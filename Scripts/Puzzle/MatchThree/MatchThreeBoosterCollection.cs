using System;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    [Serializable]
    public class MatchThreeBoosterCollection
    {
        [Header("Boosters")]
        [SerializeField] private MatchThreeRocket HorizontalRocket;
        [SerializeField] private MatchThreeRocket VerticalRocket;
        [SerializeField] private MatchThreeBomb Bomb;

        public MatchThreeElement GetBooster(MatchGroup.MatchGroupShapes shape)
        {
            switch(shape)
            {
                case MatchGroup.MatchGroupShapes.Horizontal:
                    return VerticalRocket;
                case MatchGroup.MatchGroupShapes.Vertical:
                    return HorizontalRocket;
                case MatchGroup.MatchGroupShapes.LShape:
                    return Bomb;
            }
            return null;
        }
    }
}