namespace Kuantech.Utils
{
    public class KtLinearFormula : KtFormula
    {
        public float A;
        public float B;
        public override float Evaluate(float input)
        {
            return A * input + B;
        }
    }
}