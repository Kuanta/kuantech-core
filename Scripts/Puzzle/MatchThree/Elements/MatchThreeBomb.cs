
using System.Collections.Generic;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeBomb : MatchThreeElement
    {
        public enum ExplosionTypes
        {
            Bomb,
            HorizontalRocket,
            VerticalRocket,
        }
        [Header("Explosion Type")]
        [SerializeField] private ExplosionTypes ExplosionType;

        [Header("Explosion Effect")]
        [SerializeField] private EffectPlayer ExplosionEffect;
        public override void Spawn(bool isExisting = false)
        {
            Interactable = true;
            base.Spawn(isExisting);
        }
        public override void Interact()
        {
            ExplosionEffect.PlayEffectAtPosition(ParentBoard.GetGlobalPosition(AnchorRow, AnchorColumn), Quaternion.identity);
            //Destroy nearby elements
            List<MatchThreeElement> elements = GetElementsToDestroy();
            foreach(var element in elements)
            {
                ParentMatchThreeBoard.DestroyElement(element);
            }
            base.Interact();
        }
        public virtual List<MatchThreeElement> GetElementsToDestroy()
        {
            switch(ExplosionType)
            {
                default:
                case ExplosionTypes.Bomb:
                    return BombExplosion();
                case ExplosionTypes.HorizontalRocket:
                    return HorizontalExplosion();
                case ExplosionTypes.VerticalRocket:
                    return VerticalExplosion();
                
            }
        }

        private List<MatchThreeElement>  BombExplosion()
        {
            List<MatchThreeElement> elements = new List<MatchThreeElement>();
            for (int r = -1; r < 2; ++r)
            {
                for (int c = -1; c < 2; ++c)
                {
                    if (r == 0 && c == 0) continue;
                    MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(AnchorRow - r, AnchorColumn - c);
                    if(element == null) continue;
                    elements.Add(element);
                }
            }
            return elements;
        }

        private List<MatchThreeElement> HorizontalExplosion()
        {
            List<MatchThreeElement> elements = new List<MatchThreeElement>();
            for (int c = 0; c < ParentMatchThreeBoard.ColumnCount; ++c)
            {
                MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(AnchorRow, c);
                if(element == null) continue;
                elements.Add(element);
            }
            return elements;
        }

        private List<MatchThreeElement> VerticalExplosion()
        {
            List<MatchThreeElement> elements = new List<MatchThreeElement>();
            for (int r = 0; r < ParentMatchThreeBoard.RowCount; ++r)
            {
                MatchThreeElement element = ParentMatchThreeBoard.GetMatchThreeElement(r, AnchorColumn);
                if (element == null) continue;
                elements.Add(element);
            }
            return elements;
        }
    }

  
}