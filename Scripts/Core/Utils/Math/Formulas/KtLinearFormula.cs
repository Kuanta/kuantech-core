namespace Kuantech.Utils
{
    public class KtLinearFormula : KtFormula
    {
        public float Scale;
        public float Base;
        public KtLinearFormula()
        {
            Scale=1;
            Base=0;
        }
        public override float Evaluate(float input)
        {
            return Scale * input + Base;
        }
    }
}