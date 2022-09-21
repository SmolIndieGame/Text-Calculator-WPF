using System;

namespace Text_Caculator_WPF
{
    internal readonly struct EvaluateResult
    {
        public readonly bool isSuccessful;
        public readonly double value;
        public readonly string errorMessage;
        public readonly int errorStartIndex;
        public readonly int errorEndIndex;

        public EvaluateResult(double value)
        {
            this.value = value;
            isSuccessful = true;
            errorMessage = string.Empty;
            errorStartIndex = -1;
            errorEndIndex = -1;
        }

        public EvaluateResult(string errorMessage, int start, int end)
        {
            this.errorMessage = errorMessage;
            errorStartIndex = start;
            errorEndIndex = end;
            isSuccessful = false;
            value = 0;
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
                return Error(e.Message, e.startIndex, e.endIndex);
            }
        }

        /*public static EvaluateResult Evaluate(BaseOperation op)
        {
            switch (op)
            {
                case UnaryOperation unaryOp:
                    if (unaryOp.inside == null)
                        return Error(ErrorMessages.InvalidOperation, op.startChar, op.endChar);

                    var evalResult = Evaluate(unaryOp.inside);
                    if (!evalResult.isSuccessful) return evalResult;
                    var opResult = unaryOp.operationHandler.Calculate(evalResult.value);
                    if (!opResult.isSuccessful) return Error(opResult.errorMessage, op.startChar, op.endChar);
                    return opResult.value;
                case BinaryOperation binaryOp:
                    if (binaryOp.left == null || binaryOp.right == null)
                        return Error(ErrorMessages.InvalidOperation, op.startChar, op.endChar);

                    var leftEvalResult = Evaluate(binaryOp.left);
                    if (!leftEvalResult.isSuccessful) return leftEvalResult;

                    var rightEvalResult = Evaluate(binaryOp.right);
                    if (!rightEvalResult.isSuccessful) return rightEvalResult;

                    opResult = binaryOp.operationHandler.Calculate(leftEvalResult.value, rightEvalResult.value);
                    if (!opResult.isSuccessful) return Error(opResult.errorMessage, op.startChar, op.endChar);
                    return opResult.value;
                case LiteralOperation literalOp:
                    return literalOp.literalValue;
                default:
                    return Error(ErrorMessages.InvalidOperation, op.startChar, op.endChar);
            }
        }*/

#if false
        static Dictionary<int, int> openToCloseParentheses = new();
        static Stack<int> openParentheses = new();
        
        public static EvaluateResult Evaluate(ReadOnlySpan<char> line)
        {
            line = line.TrimEnd();
            if (!BuildParenthesesMap(line))
                return Error(ErrorMessages.TooManyBackets, line.Length - 1);
            return Evaluate(line, 0);
        }

        static bool BuildParenthesesMap(ReadOnlySpan<char> textSpan)
        {
            openParentheses.Clear();
            openToCloseParentheses.Clear();

            for (int i = 0; i < textSpan.Length; i++)
            {
                if (textSpan[i] == '(')
                    openParentheses.Push(i);
                
                if (textSpan[i] != ')')
                    continue;
                if (!openParentheses.TryPop(out var openParenthesesIndex))
                    return false;

                openToCloseParentheses.Add(openParenthesesIndex, i);
            }
            while (openParentheses.TryPop(out var openParenthesesIndex))
                openToCloseParentheses.Add(openParenthesesIndex, textSpan.Length);
            return true;
        }

        static EvaluateResult Evaluate(ReadOnlySpan<char> textSpan, int start, bool twoNumberInARow = false)
        {
            int trimStart = 0;
            while (trimStart < textSpan.Length && char.IsWhiteSpace(textSpan[trimStart]))
                trimStart++;
            textSpan = textSpan[trimStart..];
            start += trimStart;
            textSpan = textSpan.TrimEnd();

            if (textSpan.IsEmpty)
                return Error(ErrorMessages.EmptyOperation, start);

            if (textSpan[0] == '(')
            {
                int close = openToCloseParentheses[start];
                if (close >= start + textSpan.Length - 1)
                    return Evaluate(textSpan[1..(close - start)], start + 1);
            }

            if (textSpan[0] == '-')
            {
                var evalResult = Evaluate(textSpan[1..], start + 1);
                if (evalResult.isSuccessful)
                    return -evalResult.value;
                return evalResult;
            }

            BaseBinaryOperationHandler? currentHandler = null;
            int currentSymbolIndex = -1;
            int symbolLength = 0;
            bool isLastSymbolAOperator = false;
            for (int i = 0; i < textSpan.Length; i++)
            {
                if (textSpan[i] == '(')
                {
                    var mulHandler = new MultiplicationHandler();
                    /*if (!isLastSymbolAOperator && (currentHandler == null || mulHandler.order <= currentHandler.order))
                    {
                        currentHandler = mulHandler;
                        currentSymbolIndex = i;
                        symbolLength = 0;
                    }*/
                    i = openToCloseParentheses[i + start] - start - 1;
                    continue;
                }

                if (SymbolConvertor.BinarySymbolToHandler.TryGetValue(textSpan[i], out var handler) && handler.order <= BaseUnaryOperationHandler.UnaryOperationOrder && (currentHandler == null || handler.order <= currentHandler.order))
                {
                    currentHandler = handler;
                    currentSymbolIndex = i;
                    symbolLength = 1;
                    isLastSymbolAOperator = true;
                    continue;
                }

                if (!char.IsWhiteSpace(textSpan[i]))
                    isLastSymbolAOperator = false;
            }

            if (currentHandler == null)
            {
                if (twoNumberInARow && textSpan[0] != '(')
                    return Error(ErrorMessages.InvalidOperation, start);
                var parseTo = ParseToNumber(textSpan, start, out var val);
                if (parseTo == 0)
                    return Error(ErrorMessages.InvalidOperation, start);

                if (parseTo >= textSpan.Length)
                    return val;
                EvaluateResult evalR = Evaluate(textSpan[parseTo..], start + parseTo, true);
                if (!evalR.isSuccessful) return evalR;
                double eval = evalR.value;
                return val * eval;
            }

            /*EvaluateResult aResult = Evaluate(textSpan[..currentSymbolIndex], start);
            double a = aResult.value;
            if (!aResult.isSuccessful)
            {
                if (aResult.errorMessage != ErrorMessages.EmptyOperation || currentHandler is not SubtractHandler)
                    return aResult;
                a = 0;
            }*/

            EvaluateResult aResult = Evaluate(textSpan[..currentSymbolIndex], start);
            if (!aResult.isSuccessful) return aResult;
            double a = aResult.value;

            EvaluateResult bResult = Evaluate(textSpan[(currentSymbolIndex + symbolLength)..], start + currentSymbolIndex + symbolLength);
            if (!bResult.isSuccessful) return bResult;
            double b = bResult.value;

            var result = currentHandler.Calculate(a, b);
            if (result.isSuccessful)
                return result.value;
            return Error(result.errorMessage, start);
        }

        /// <returns>The length of the part of the textSpan that is parsable.</returns>
        static int ParseToNumber(ReadOnlySpan<char> textSpan, int start, out double value)
        {
            value = 0;
            if (textSpan[0] == ',') return 0;

            int lastValidDigit = -1;
            int decimalPointDepth = -1;

            int backetsDepth = 0;
            bool closingBackets = textSpan[0] != '(';

            for (int i = 0; i < textSpan.Length; i++)
            {
                char c = textSpan[i];
                switch (c)
                {
                    case ',':
                        continue;
                    case '(' when !closingBackets:
                        backetsDepth++;
                        continue;
                    case ')' when backetsDepth > 0:
                        lastValidDigit = i;

                        backetsDepth--;
                        closingBackets = true;
                        if (backetsDepth > 0)
                            continue;
                        else
                            break;
                    case '.' when decimalPointDepth == -1:
                        decimalPointDepth = 0;
                        continue;
                    case >= '0' and <= '9':
                        lastValidDigit = i;

                        value *= 10;
                        value += c - '0';
                        if (decimalPointDepth >= 0)
                            decimalPointDepth++;
                        continue;
                    default:
                        break;
                }
                break;
            }

            if (decimalPointDepth > 0)
                value /= Math.Pow(10, decimalPointDepth);

            return lastValidDigit + 1;
        }
#endif

        static EvaluateResult Error(string message, int start, int end)
        {
            return new EvaluateResult(message, start, end);
        }
    }
}
