using System;

namespace Text_Calculator_WPF;

internal readonly struct EvaluateResult
{
    public readonly bool IsSuccessful;
    public readonly double Value;
    public readonly string ErrorMessage;
    public readonly int ErrorStartIndex;
    public readonly int ErrorEndIndex;

    public EvaluateResult(double value)
    {
        Value = value;
        IsSuccessful = true;
        ErrorMessage = string.Empty;
        ErrorStartIndex = -1;
        ErrorEndIndex = -1;
    }

    public EvaluateResult(string errorMessage, int start, int end)
    {
        ErrorMessage = errorMessage;
        ErrorStartIndex = start;
        ErrorEndIndex = end;
        IsSuccessful = false;
        Value = 0;
    }

    public static implicit operator EvaluateResult(double value) => new(value);
}

internal static class Evaluator
{
    public static EvaluateResult Evaluate(ReadOnlySpan<char> line)
    {
        line = line.TrimEnd();
        try
        {
            BaseOperation op = SyntaxAnalyzer.Analysis(line);
            return op.Evaluate();
        }
        catch (SyntaxException e)
        {
            return Error(e.Message, e.StartIndex, e.EndIndex);
        }
    }

    static EvaluateResult Error(string message, int start, int end)
    {
        return new EvaluateResult(message, start, end);
    }
}
