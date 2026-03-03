using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Text_Caculator_WPF
{
    [Serializable]
    public class SyntaxException : Exception
    {
        public int startIndex { get; }
        public int endIndex { get; }

        public SyntaxException(string message, int start, int end) : base(message)
        {
            startIndex = start;
            endIndex = end;
        }
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

        static Stack<BaseOperation> constructingOperations = new();
        static Stack<BaseOperation> finishedOperations = new();

        static WordType lastWordType;
        public static BaseOperation Analysis(ReadOnlySpan<char> text)
        {
            constructingOperations.Clear();
            finishedOperations.Clear();

            int wordStartIndex = 0;
            int layer = 0;

            double number = 0;
            int lastValidDigit = -1;
            int decimalPointDepth = -1;

            ConstructingWordType constructingWord = ConstructingWordType.None;
            lastWordType = WordType.Start;

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
                    finishedOperations.Push(new LiteralOperation(number) { startChar = wordStartIndex, endChar = i });
                    lastWordType = WordType.Number;

                    constructingWord = ConstructingWordType.None;
                    return;
                }

                if (constructingWord != ConstructingWordType.Word)
                    return;

                var uOphandler = SymbolConvertor.NameToUnaryHandler(text[wordStartIndex..i]);
                if (uOphandler is not null)
                {
                    if (!uOphandler.isRightSide && lastWordType != WordType.Start && lastWordType != WordType.ConstructingOp)
                        AddBinaryOperation(layer, SymbolConvertor.GetConnectorFor(uOphandler), wordStartIndex, i);
                    AddUnaryOperation(layer, uOphandler, wordStartIndex, i);
                    constructingWord = ConstructingWordType.None;
                    return;
                }

                var biOphandler = SymbolConvertor.NameToBinaryHandler(text[wordStartIndex..i]);
                if (biOphandler is not null)
                {
                    AddBinaryOperation(layer, biOphandler, wordStartIndex, i);
                    constructingWord = ConstructingWordType.None;
                    return;
                }

                var userVar = SymbolConvertor.NameToConstant(text[wordStartIndex..i]);
                if (double.IsNaN(userVar))
                    userVar = SymbolConvertor.NameToUserVariable(text[wordStartIndex..i]);
                SyntaxThrower.ThrowIf(double.IsNaN(userVar), ErrorMessages.UnknownWord, wordStartIndex, i);

                if (lastWordType != WordType.Start && lastWordType != WordType.ConstructingOp)
                    AddBinaryOperation(layer, SymbolConvertor.GetMulConnector(), wordStartIndex, i);
                finishedOperations.Push(new LiteralOperation(userVar) { startChar = wordStartIndex, endChar = i });
                lastWordType = WordType.FinishedOp;
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
                        SyntaxThrower.ThrowIf(lastWordType == WordType.Number || lastWordType == WordType.FinishedOp, ErrorMessages.MissingBinaryOp, i);
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
                        if (lastWordType == WordType.Number || lastWordType == WordType.FinishedOp)
                        {
                            AddBinaryOperation(layer, SymbolConvertor.GetMulConnector(), i, i + 1);
                            layer++;
                            continue;
                        }

                        // This execution order fix is for matching with what a human would expect.
                        if (lastWordType == WordType.ConstructingOp && constructingOperations.Peek() is UnaryOperation oldOp && oldOp.operationHandler.Symbol.Length != 1)
                        {
                            constructingOperations.Pop();
                            constructingOperations.Push(new UnaryOperation(oldOp.operationHandler) { layer = oldOp.layer, order = newOrderForBracketedUnaryOp, startChar = oldOp.startChar });
                        }

                        layer++;
                        continue;
                    case ')':
                        SyntaxThrower.ThrowIf(layer == 0, ErrorMessages.TooManyBackets, i);

                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(text, i);
                        SyntaxThrower.ThrowIf(lastWordType == WordType.Start || lastWordType == WordType.ConstructingOp, ErrorMessages.InvalidOperation, i);

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
                        var uOphandler = SymbolConvertor.SymbolToUnaryHandler(c);
                        if (uOphandler is not null)
                        {
                            if (constructingWord != ConstructingWordType.None)
                                FinishConstructWord(text, i);
                            if (!uOphandler.isRightSide && lastWordType != WordType.Start && lastWordType != WordType.ConstructingOp)
                                AddBinaryOperation(layer, SymbolConvertor.GetConnectorFor(uOphandler), i, i + 1);
                            AddUnaryOperation(layer, uOphandler, i, i + 1);
                            continue;
                        }

                        var biOphandler = SymbolConvertor.SymbolToBinaryHandler(c);
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
            SyntaxThrower.ThrowIf(lastWordType == WordType.ConstructingOp, ErrorMessages.InvalidOperation, wordStartIndex, text.Length);
            while (constructingOperations.TryPop(out var op) && TryFinishOp(op))
                ;

            SyntaxThrower.ThrowIf(finishedOperations.Count != 1, ErrorMessages.InvalidOperation, text.Length - 1);
            return finishedOperations.Pop();
        }

        private static void AddUnaryOperation(int layer, BaseUnaryOperationHandler handler, int start, int end)
        {
            SyntaxThrower.ThrowIf(handler.isRightSide && lastWordType != WordType.Number && lastWordType != WordType.FinishedOp, ErrorMessages.InvalidOperation, start, end);

            if (!handler.isRightSide)
            {
                constructingOperations.Push(new UnaryOperation(handler) { startChar = start, layer = layer, order = handler.Order });
                lastWordType = WordType.ConstructingOp;
                return;
            }

            while (constructingOperations.Count >= 1)
            {
                var beforeOp = constructingOperations.Peek();
                bool orderCondition = beforeOp.order >= handler.Order;
                if (beforeOp.layer <= layer && (beforeOp.layer != layer || !orderCondition))
                    break;

                var op = constructingOperations.Pop();
                SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, start, end);
            }
            SyntaxThrower.ThrowIf(!TryFinishOp(new UnaryOperation(handler) { endChar = end, layer = layer, order = handler.Order }), ErrorMessages.InvalidOperation, start, end);
            lastWordType = WordType.FinishedOp;
        }

        private static void AddBinaryOperation(int layer, BaseBinaryOperationHandler handler, int start, int end)
        {
            SyntaxThrower.ThrowIf(lastWordType != WordType.Number && lastWordType != WordType.FinishedOp, ErrorMessages.InvalidOperation, start, end);

            while (constructingOperations.Count >= 1)
            {
                var beforeOp = constructingOperations.Peek();
                bool orderCondition = handler.leftToRight ? beforeOp.order >= handler.Order : beforeOp.order > handler.Order;
                if (beforeOp.layer <= layer && (beforeOp.layer != layer || !orderCondition))
                    break;

                var op = constructingOperations.Pop();
                SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, start, end);
            }

            constructingOperations.Push(new BinaryOperation(handler) { layer = layer, order = handler.Order });
            lastWordType = WordType.ConstructingOp;
        }

        private static bool TryFinishOp(BaseOperation op)
        {
            if (op is BinaryOperation biOp)
            {
                if (!finishedOperations.TryPop(out var subOp2) || !finishedOperations.TryPop(out var subOp1))
                    return false;

                SyntaxThrower.ThrowIf(subOp1.endChar > subOp2.startChar, ErrorMessages.InvalidOperation, subOp2.startChar);

                biOp.left = subOp1;
                biOp.right = subOp2;
                op.startChar = subOp1.startChar;
                op.endChar = subOp2.endChar;
            }
            if (op is UnaryOperation uOp)
            {
                if (!finishedOperations.TryPop(out var subOp))
                    return false;

                if (uOp.operationHandler.isRightSide)
                {
                    SyntaxThrower.ThrowIf(subOp.endChar > op.endChar, ErrorMessages.InvalidOperation, op.startChar);
                    op.startChar = subOp.startChar;
                }
                else
                {
                    SyntaxThrower.ThrowIf(op.startChar > subOp.startChar, ErrorMessages.InvalidOperation, subOp.startChar);
                    op.endChar = subOp.endChar;
                }
                uOp.inside = subOp;
            }

            finishedOperations.Push(op);
            return true;
        }
    }
}
