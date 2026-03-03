namespace Text_Caculator_WPF
{
    internal sealed class UnaryOperation : BaseOperation
    {
        public UnaryOperation(BaseUnaryOperationHandler operationHandler)
        {
            this.operationHandler = operationHandler;
        }

        public BaseUnaryOperationHandler operationHandler { get; }
        public BaseOperation? inside { get; set; }

        public override EvaluateResult Evaluate()
        {
            if (inside == null)
                return new EvaluateResult(ErrorMessages.InvalidOperation, startChar, endChar);

            var evalResult = inside.Evaluate();
            if (!evalResult.isSuccessful) return evalResult;
            var result = operationHandler.Calculate(evalResult.value);
            if (!result.isSuccessful) return new EvaluateResult(result.errorMessage, startChar, endChar);
            if (double.IsInfinity(result.value)) return new EvaluateResult(ErrorMessages.ResultTooLarge, startChar, endChar);
            return result.value;
        }
    }
}
