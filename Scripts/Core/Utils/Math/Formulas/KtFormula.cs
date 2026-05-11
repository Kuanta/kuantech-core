using System;

namespace Kuantech.Utils
{
    [Serializable]
    public abstract class KtFormula
    {
        public abstract float Evaluate(float input);
    }
}