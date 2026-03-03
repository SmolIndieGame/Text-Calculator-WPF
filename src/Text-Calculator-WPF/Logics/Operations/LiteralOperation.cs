namespace Text_Calculator_WPF;

internal sealed class LiteralOperation(double literalValue) : BaseOperation
{
    public double LiteralValue { get; } = literalValue;

    public override EvaluateResult Evaluate() => LiteralValue;
}
