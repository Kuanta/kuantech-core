
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeBomb : MatchThreeElement
    {
        [Header("Explosion Effect")]
        [SerializeField] private Effect ExplosionEffect;
        public override void Interact()
        {
            //Destroy nearby elements
            for(int r=-1;r<2;++r)
            {
                for(int c=-1;c<2;++c)
                {
                    if(r == 0 && c==0) continue;
                    ParentMatchThreeBoard.DestroyElement(Row-r, Column-c);
                }
            }
            base.Interact();
        }
    }
}