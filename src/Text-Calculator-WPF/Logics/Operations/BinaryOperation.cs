namespace Text_Calculator_WPF;

internal sealed class BinaryOperation(BaseBinaryOperationHandler operationHandler) : BaseOperation
{
    public BaseBinaryOperationHandler OperationHandler { get; } = operationHandler;
    public BaseOperation? Left { get; set; }
    public BaseOperation? Right { get; set; }

    public override EvaluateResult Evaluate()
    {
        if (Left == null || Right == null)
            return new EvaluateResult(ErrorMessages.InvalidOperation, StartChar, EndChar);

        var leftEvalResult = Left.Evaluate();
        if (!leftEvalResult.IsSuccessful) return leftEvalResult;

        var rightEvalResult = Right.Evaluate();
        if (!rightEvalResult.IsSuccessful) return rightEvalResult;

        var result = OperationHandler.Calculate(leftEvalResult.Value, rightEvalResult.Value);
        if (!result.IsSuccessful) return new EvaluateResult(result.ErrorMessage, StartChar, EndChar);
        if (double.IsInfinity(result.Value)) return new EvaluateResult(ErrorMessages.ResultTooLarge, StartChar, EndChar);
        return result.Value;
    }
}
