namespace Text_Caculator_WPF
{
    internal sealed class BinaryOperation : BaseOperation
    {
        public BinaryOperation(BaseBinaryOperationHandler operationHandler)
        {
            this.operationHandler = operationHandler;
        }

        public BaseBinaryOperationHandler operationHandler { get; }
        public BaseOperation? left { get; set; }
        public BaseOperation? right { get; set; }

        public override EvaluateResult Evaluate()
        {
            if (left == null || right == null)
                return new EvaluateResult(ErrorMessages.InvalidOperation, startChar, endChar);

            var leftEvalResult = left.Evaluate();
            if (!leftEvalResult.isSuccessful) return leftEvalResult;

            var rightEvalResult = right.Evaluate();
            if (!rightEvalResult.isSuccessful) return rightEvalResult;

            var result = operationHandler.Calculate(leftEvalResult.value, rightEvalResult.value);
            if (!result.isSuccessful) return new EvaluateResult(result.errorMessage, startChar, endChar);
            if (double.IsInfinity(result.value)) return new EvaluateResult(ErrorMessages.ResultTooLarge, startChar, endChar);
            return result.value;
        }
    }
}
