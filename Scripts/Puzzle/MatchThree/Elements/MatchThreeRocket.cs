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
        [SerializeField] private Effect HorizontalRocketEffect;
        [SerializeField] private Effect VerticalRocketEffect;

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
                ParentBoard.PlayEffect(HorizontalRocketEffect, Row, Column);
            }

            if(Vertical)
            {
                for (int r = 0; r < ParentMatchThreeBoard.RowCount; ++r)
                {
                    MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(r, Column);
                    ParentMatchThreeBoard.DestroyElement(element);
                }
                ParentBoard.PlayEffect(VerticalRocketEffect, Row, Column);
            }

            base.Interact();
        }
    }
}