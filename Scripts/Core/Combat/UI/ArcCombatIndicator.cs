using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class ArcCombatIndicator : CombatIndicator
    {
        public override CombatIndicatorType Type => CombatIndicatorType.ARC;
        [SerializeField] private ArcRegion _arcRegion;

        protected override void Setup(CombatIndicatorData data)
        {
            _arcRegion.Radius = data.Range;
            _arcRegion.Arc    = data.Angle;
            _arcRegion.FillProgress = 0f;
        }

        protected override void SetFill(float fill)
        {
            _arcRegion.FillProgress = fill;
        }
    }
}
