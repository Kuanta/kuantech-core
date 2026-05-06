using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class CircleCombatIndicator : CombatIndicator
    {
        public override CombatIndicatorType Type => CombatIndicatorType.CIRCLE;
        [SerializeField] private CircleRegion _circleRegion;

        protected override void Setup(CombatIndicatorData data)
        {
            _circleRegion.Radius       = data.Range;
            _circleRegion.FillProgress = 0f;
        }

        protected override void SetFill(float fill)
        {
            _circleRegion.FillProgress = fill;
        }
    }
}
