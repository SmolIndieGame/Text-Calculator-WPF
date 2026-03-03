namespace Text_Caculator_WPF
{
    internal sealed class LiteralOperation : BaseOperation
    {
        public LiteralOperation(double literalValue)
        {
            this.literalValue = literalValue;
        }

        public double literalValue { get; }

        public override EvaluateResult Evaluate() => literalValue;
    }
}
