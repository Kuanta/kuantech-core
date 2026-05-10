using System;
using UnityEngine;

namespace Kuantech.Utils
{
    /// <summary>
    /// f(x) = Cap * (1 - e^(-k * x))
    /// Starts at 0, asymptotically approaches Cap. Each point of input yields diminishing returns.
    /// Useful for resistance stats (e.g. Endurance → PhysicalResistance).
    /// </summary>
    [Serializable]
    public class KtExponentialDecayFormula : KtFormula
    {
        [Tooltip("Maximum value the formula can approach (asymptote)")]
        public float Cap = 1f;
        [Tooltip("Controls how quickly the curve rises. Higher = faster early growth")]
        public float K = 0.1f;

        public override float Evaluate(float input)
        {
            return Cap * (1f - Mathf.Exp(-K * input));
        }
    }
}
