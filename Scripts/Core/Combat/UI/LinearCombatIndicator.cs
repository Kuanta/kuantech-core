using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class LinearCombatIndicator : CombatIndicator
    {
        public override CombatIndicatorType Type => CombatIndicatorType.LINEAR;
        [SerializeField] private LineRegion _lineRegion;

        protected override void Setup(CombatIndicatorData data)
        {
            _lineRegion.Length       = data.Range;
            _lineRegion.Width        = data.Width;
            _lineRegion.FillProgress = 0f;
        }

        protected override void SetFill(float fill)
        {
            _lineRegion.FillProgress = fill;
        }
    }
}
