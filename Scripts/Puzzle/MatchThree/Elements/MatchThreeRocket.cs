using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeRocket : MatchThreeElement
    {
        [Header("Rocket Properties")]
        public bool Horizontal;
        public bool Vertical;

        [Header("Effect")]
        [SerializeField] private EffectPlayer HorizontalRocketEffect;
        [SerializeField] private EffectPlayer VerticalRocketEffect;

        public override void Spawn()
        {
            Interactable = true;
            base.Spawn();
        }
        public override void Interact()
        {
            if(Horizontal)
            {
                for(int c=0;c<ParentMatchThreeBoard.ColumnCount;++c)
                {
                    MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(Row, c);
                    ParentMatchThreeBoard.DestroyElement(element);
                }
                HorizontalRocketEffect?.PlayEffectAtPosition(ParentBoard.GetGlobalPosition(Row, Column), Quaternion.identity);
            }

            if(Vertical)
            {
                for (int r = 0; r < ParentMatchThreeBoard.RowCount; ++r)
                {
                    MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(r, Column);
                    ParentMatchThreeBoard.DestroyElement(element);
                }
                VerticalRocketEffect?.PlayEffectAtPosition(ParentBoard.GetGlobalPosition(Row, Column), Quaternion.identity);
            }

            base.Interact();
        }
    }
}