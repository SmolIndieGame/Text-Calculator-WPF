namespace Text_Calculator_WPF;

internal abstract class BaseOperation
{
    public int StartChar { get; set; } = -1;
    public int EndChar { get; set; } = -1;

    public int Layer { get; init; } = -1;
    public int Order { get; init; } = -1;

    public abstract EvaluateResult Evaluate();
}
