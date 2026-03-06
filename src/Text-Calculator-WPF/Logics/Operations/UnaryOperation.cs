namespace Text_Calculator_WPF;

internal sealed class UnaryOperation(BaseUnaryOperationHandler operationHandler) : BaseOperation
{
    public BaseUnaryOperationHandler OperationHandler { get; } = operationHandler;
    public BaseOperation? Inside { get; set; }

    public override EvaluateResult Evaluate()
    {
        if (Inside == null)
            return new EvaluateResult(ErrorMessages.InvalidOperation, StartChar, EndChar);

        var evalResult = Inside.Evaluate();
        if (!evalResult.IsSuccessful) return evalResult;
        var result = OperationHandler.Calculate(evalResult.Value);
        if (!result.IsSuccessful) return new EvaluateResult(result.ErrorMessage, StartChar, EndChar);
        if (double.IsInfinity(result.Value)) return new EvaluateResult(ErrorMessages.ResultTooLarge, StartChar, EndChar);
        return result.Value;
    }
}
