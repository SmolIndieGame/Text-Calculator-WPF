namespace Text_Calculator_WPF;

public readonly struct OperationResult
{
    public readonly bool IsSuccessful;
    public readonly double Value;
    public readonly string ErrorMessage;

    public OperationResult(double value)
    {
        Value = value;
        IsSuccessful = true;
        ErrorMessage = string.Empty;
    }

    public OperationResult(string errorMessage)
    {
        ErrorMessage = errorMessage;
        IsSuccessful = false;
        Value = 0;
    }

    public static implicit operator OperationResult(double value) => new(value);
    public static implicit operator OperationResult(string errorMessage) => new(errorMessage);
}