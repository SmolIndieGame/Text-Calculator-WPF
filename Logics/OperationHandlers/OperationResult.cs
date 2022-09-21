namespace Text_Caculator_WPF
{
    public readonly struct OperationResult
    {
        public readonly bool isSuccessful;
        public readonly double value;
        public readonly string errorMessage;

        public OperationResult(double value)
        {
            this.value = value;
            isSuccessful = true;
            errorMessage = string.Empty;
        }

        public OperationResult(string errorMessage)
        {
            this.errorMessage = errorMessage;
            isSuccessful = false;
            value = 0;
        }

        public static implicit operator OperationResult(double value) => new(value);
        public static implicit operator OperationResult(string errorMessage) => new(errorMessage);
    }
}