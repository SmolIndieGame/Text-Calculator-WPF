using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Text_Calculator_WPF;

[Serializable]
public class SyntaxException(string message, int start, int end) : Exception(message)
{
    public int StartIndex { get; } = start;
    public int EndIndex { get; } = end;
}

internal static class SyntaxAnalyzer
{
    enum WordType
    {
        /// <summary> Indicate this is the start of the line. </summary>
        Start,
        Number,
        FinishedOp,
        ConstructingOp
    }

    enum ConstructingWordType
    {
        None,
        Number,
        Word
    }

    const int newOrderForBracketedUnaryOp = 5;

    static Stack<BaseOperation> _constructingOperations = new();
    static Stack<BaseOperation> _finishedOperations = new();

    static WordType _lastWordType;
    public static BaseOperation Analysis(ReadOnlySpan<char> text)
    {
        _constructingOperations.Clear();
        _finishedOperations.Clear();

        int wordStartIndex = 0;
        int layer = 0;

        double number = 0;
        int lastValidDigit = -1;
        int decimalPointDepth = -1;

        ConstructingWordType constructingWord = ConstructingWordType.None;
        _lastWordType = WordType.Start;

        void StartConstructWord(int i, ConstructingWordType constructingWordType)
        {
            Debug.Assert(constructingWord == ConstructingWordType.None);

            constructingWord = constructingWordType;
            wordStartIndex = i;

            number = 0;
            lastValidDigit = -1;
            decimalPointDepth = -1;
        }

        void FinishConstructWord(ReadOnlySpan<char> text, int i)
        {
            Debug.Assert(constructingWord != ConstructingWordType.None);

            if (wordStartIndex >= i)
            {
                constructingWord = ConstructingWordType.None;
                return;
            }

            if (constructingWord == ConstructingWordType.Number)
            {
                SyntaxThrower.ThrowIf(lastValidDigit != i - 1, ErrorMessages.UnknownWord, wordStartIndex, i);

                if (decimalPointDepth > 0)
                    number /= Math.Pow(10, decimalPointDepth);
                _finishedOperations.Push(new LiteralOperation(number) { StartChar = wordStartIndex, EndChar = i });
                _lastWordType = WordType.Number;

                constructingWord = ConstructingWordType.None;
                return;
            }

            if (constructingWord != ConstructingWordType.Word)
                return;

            var uOphandler = SymbolConverter.NameToUnaryHandler(text[wordStartIndex..i]);
            if (uOphandler is not null)
            {
                if (!uOphandler.IsRightSide && _lastWordType != WordType.Start && _lastWordType != WordType.ConstructingOp)
                    AddBinaryOperation(layer, SymbolConverter.GetConnectorFor(uOphandler), wordStartIndex, i);
                AddUnaryOperation(layer, uOphandler, wordStartIndex, i);
                constructingWord = ConstructingWordType.None;
                return;
            }

            var biOphandler = SymbolConverter.NameToBinaryHandler(text[wordStartIndex..i]);
            if (biOphandler is not null)
            {
                AddBinaryOperation(layer, biOphandler, wordStartIndex, i);
                constructingWord = ConstructingWordType.None;
                return;
            }

            var userVar = SymbolConverter.NameToConstant(text[wordStartIndex..i]);
            if (double.IsNaN(userVar))
                userVar = SymbolConverter.NameToUserVariable(text[wordStartIndex..i]);
            SyntaxThrower.ThrowIf(double.IsNaN(userVar), ErrorMessages.UnknownWord, wordStartIndex, i);

            if (_lastWordType != WordType.Start && _lastWordType != WordType.ConstructingOp)
                AddBinaryOperation(layer, SymbolConverter.GetMulConnector(), wordStartIndex, i);
            _finishedOperations.Push(new LiteralOperation(userVar) { StartChar = wordStartIndex, EndChar = i });
            _lastWordType = WordType.FinishedOp;
            constructingWord = ConstructingWordType.None;
        }

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                if (constructingWord != ConstructingWordType.None)
                    FinishConstructWord(text, i);
                continue;
            }

            if (c == '.' || c >= '0' && c <= '9')
            {
                if (constructingWord == ConstructingWordType.Word)
                    FinishConstructWord(text, i);
                if (constructingWord == ConstructingWordType.None)
                {
                    SyntaxThrower.ThrowIf(_lastWordType == WordType.Number || _lastWordType == WordType.FinishedOp, ErrorMessages.MissingBinaryOp, i);
                    StartConstructWord(i, ConstructingWordType.Number);
                }
            }

            if (c == ',' && constructingWord == ConstructingWordType.Number)
                continue;

            switch (c)
            {
                case '(':
                    if (constructingWord != ConstructingWordType.None)
                        FinishConstructWord(text, i);
                    if (_lastWordType == WordType.Number || _lastWordType == WordType.FinishedOp)
                    {
                        AddBinaryOperation(layer, SymbolConverter.GetMulConnector(), i, i + 1);
                        layer++;
                        continue;
                    }

                    // This execution order fix is for matching with what a human would expect.
                    if (_lastWordType == WordType.ConstructingOp && _constructingOperations.Peek() is UnaryOperation oldOp && oldOp.OperationHandler.Symbol.Length != 1)
                    {
                        _constructingOperations.Pop();
                        _constructingOperations.Push(new UnaryOperation(oldOp.OperationHandler) { Layer = oldOp.Layer, Order = newOrderForBracketedUnaryOp, StartChar = oldOp.StartChar });
                    }

                    layer++;
                    continue;
                case ')':
                    SyntaxThrower.ThrowIf(layer == 0, ErrorMessages.TooManyBackets, i);

                    if (constructingWord != ConstructingWordType.None)
                        FinishConstructWord(text, i);
                    SyntaxThrower.ThrowIf(_lastWordType == WordType.Start || _lastWordType == WordType.ConstructingOp, ErrorMessages.InvalidOperation, i);

                    layer--;
                    continue;
                case '.':
                    SyntaxThrower.ThrowIf(decimalPointDepth != -1, ErrorMessages.TooManyDecimalPoint, i);

                    decimalPointDepth = 0;
                    continue;
                case >= '0' and <= '9':
                    lastValidDigit = i;

                    number *= 10;
                    number += c - '0';
                    if (decimalPointDepth >= 0)
                        decimalPointDepth++;
                    continue;
                default:
                    var uOphandler = SymbolConverter.SymbolToUnaryHandler(c);
                    if (uOphandler is not null)
                    {
                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(text, i);
                        if (!uOphandler.IsRightSide && _lastWordType != WordType.Start && _lastWordType != WordType.ConstructingOp)
                            AddBinaryOperation(layer, SymbolConverter.GetConnectorFor(uOphandler), i, i + 1);
                        AddUnaryOperation(layer, uOphandler, i, i + 1);
                        continue;
                    }

                    var biOphandler = SymbolConverter.SymbolToBinaryHandler(c);
                    if (biOphandler is not null)
                    {
                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(text, i);
                        AddBinaryOperation(layer, biOphandler, i, i + 1);
                        continue;
                    }

                    if (constructingWord == ConstructingWordType.Number)
                        FinishConstructWord(text, i);
                    if (constructingWord == ConstructingWordType.None)
                        StartConstructWord(i, ConstructingWordType.Word);
                    continue;
            }
        }

        if (constructingWord != ConstructingWordType.None)
            FinishConstructWord(text, text.Length);
        SyntaxThrower.ThrowIf(_lastWordType == WordType.ConstructingOp, ErrorMessages.InvalidOperation, wordStartIndex, text.Length);
        while (_constructingOperations.TryPop(out var op) && TryFinishOp(op))
            ;

        SyntaxThrower.ThrowIf(_finishedOperations.Count != 1, ErrorMessages.InvalidOperation, text.Length - 1);
        return _finishedOperations.Pop();
    }

    private static void AddUnaryOperation(int layer, BaseUnaryOperationHandler handler, int start, int end)
    {
        SyntaxThrower.ThrowIf(handler.IsRightSide && _lastWordType != WordType.Number && _lastWordType != WordType.FinishedOp, ErrorMessages.InvalidOperation, start, end);

        if (!handler.IsRightSide)
        {
            _constructingOperations.Push(new UnaryOperation(handler) { StartChar = start, Layer = layer, Order = handler.Order });
            _lastWordType = WordType.ConstructingOp;
            return;
        }

        while (_constructingOperations.Count >= 1)
        {
            var beforeOp = _constructingOperations.Peek();
            bool orderCondition = beforeOp.Order >= handler.Order;
            if (beforeOp.Layer <= layer && (beforeOp.Layer != layer || !orderCondition))
                break;

            var op = _constructingOperations.Pop();
            SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, start, end);
        }
        SyntaxThrower.ThrowIf(!TryFinishOp(new UnaryOperation(handler) { EndChar = end, Layer = layer, Order = handler.Order }), ErrorMessages.InvalidOperation, start, end);
        _lastWordType = WordType.FinishedOp;
    }

    private static void AddBinaryOperation(int layer, BaseBinaryOperationHandler handler, int start, int end)
    {
        SyntaxThrower.ThrowIf(_lastWordType != WordType.Number && _lastWordType != WordType.FinishedOp, ErrorMessages.InvalidOperation, start, end);

        while (_constructingOperations.Count >= 1)
        {
            var beforeOp = _constructingOperations.Peek();
            bool orderCondition = handler.LeftToRight ? beforeOp.Order >= handler.Order : beforeOp.Order > handler.Order;
            if (beforeOp.Layer <= layer && (beforeOp.Layer != layer || !orderCondition))
                break;

            var op = _constructingOperations.Pop();
            SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, start, end);
        }

        _constructingOperations.Push(new BinaryOperation(handler) { Layer = layer, Order = handler.Order });
        _lastWordType = WordType.ConstructingOp;
    }

    private static bool TryFinishOp(BaseOperation op)
    {
        if (op is BinaryOperation biOp)
        {
            if (!_finishedOperations.TryPop(out var subOp2) || !_finishedOperations.TryPop(out var subOp1))
                return false;

            SyntaxThrower.ThrowIf(subOp1.EndChar > subOp2.StartChar, ErrorMessages.InvalidOperation, subOp2.StartChar);

            biOp.Left = subOp1;
            biOp.Right = subOp2;
            op.StartChar = subOp1.StartChar;
            op.EndChar = subOp2.EndChar;
        }
        if (op is UnaryOperation uOp)
        {
            if (!_finishedOperations.TryPop(out var subOp))
                return false;

            if (uOp.OperationHandler.IsRightSide)
            {
                SyntaxThrower.ThrowIf(subOp.EndChar > op.EndChar, ErrorMessages.InvalidOperation, op.StartChar);
                op.StartChar = subOp.StartChar;
            }
            else
            {
                SyntaxThrower.ThrowIf(op.StartChar > subOp.StartChar, ErrorMessages.InvalidOperation, subOp.StartChar);
                op.EndChar = subOp.EndChar;
            }
            uOp.Inside = subOp;
        }

        _finishedOperations.Push(op);
        return true;
    }
}
