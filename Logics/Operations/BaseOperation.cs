namespace Text_Caculator_WPF
{
    internal abstract class BaseOperation
    {
        public int startChar { get; set; } = -1;
        public int endChar { get; set; } = -1;

        public int layer { get; init; } = -1;
        public int order { get; init; } = -1;

        public abstract EvaluateResult Evaluate();
    }
}
