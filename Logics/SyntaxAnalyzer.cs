using System;
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
            None,
            Number,
            VarName,
            UnaryOp
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

        public static BaseOperation Analysis(ReadOnlySpan<char> text)
        {
            constructingOperations.Clear();
            finishedOperations.Clear();

            int wordStartIndex = 0;
            int layer = 0;

            double number = 0;
            int lastValidDigit = -1;
            int decimalPointDepth = -1;

            WordType lastWord = WordType.None;
            ConstructingWordType constructingWord = ConstructingWordType.None;

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
                    lastWord = WordType.Number;
                }

                if (constructingWord == ConstructingWordType.Word)
                {
                    var handler = SymbolConvertor.SymbolToUnaryHandler(text[wordStartIndex..i]);
                    SyntaxThrower.ThrowIfNull(handler, ErrorMessages.UnknownWord, wordStartIndex, i);
                    SyntaxThrower.ThrowIf(handler.isRightSide && lastWord != WordType.Number && lastWord != WordType.VarName, ErrorMessages.InvalidOperation, wordStartIndex, i);

                    if (!handler.isRightSide && lastWord != WordType.None && lastWord != WordType.UnaryOp)
                        AddBinaryOperation(layer, handler is NegateHandler ? SymbolConvertor.additionHandler : SymbolConvertor.multiplicationHandler, i);

                    if (!handler.isRightSide)
                    {
                        constructingOperations.Push(new UnaryOperation(handler) { startChar = wordStartIndex, layer = layer, order = handler.Order });
                        lastWord = WordType.UnaryOp;
                    }
                    else
                    {
                        while (constructingOperations.Count >= 1)
                        {
                            var beforeOp = constructingOperations.Peek();
                            bool orderCondition = beforeOp.order >= handler.Order;
                            if (beforeOp.layer <= layer && (beforeOp.layer != layer || !orderCondition))
                                break;

                            var op = constructingOperations.Pop();
                            SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, wordStartIndex, i);
                        }
                        SyntaxThrower.ThrowIf(!TryFinishOp(new UnaryOperation(handler) { endChar = i, layer = layer, order = handler.Order }), ErrorMessages.InvalidOperation, wordStartIndex, i);
                        lastWord = WordType.VarName;
                    }
                }

                constructingWord = ConstructingWordType.None;
            }

            static void AddBinaryOperation(int layer, BaseBinaryOperationHandler handler, int indexForError)
            {
                SyntaxThrower.ThrowIf(finishedOperations.Count < 1, ErrorMessages.EmptyOperation, indexForError);

                while (constructingOperations.Count >= 1)
                {
                    var beforeOp = constructingOperations.Peek();
                    bool orderCondition = handler.leftToRight ? beforeOp.order >= handler.Order : beforeOp.order > handler.Order;
                    if (beforeOp.layer <= layer && (beforeOp.layer != layer || !orderCondition))
                        break;

                    var op = constructingOperations.Pop();
                    SyntaxThrower.ThrowIf(!TryFinishOp(op), ErrorMessages.InvalidOperation, indexForError);
                }

                constructingOperations.Push(new BinaryOperation(handler) { layer = layer, order = handler.Order });
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
                        SyntaxThrower.ThrowIf(lastWord == WordType.Number || lastWord == WordType.VarName, ErrorMessages.MissingBinaryOp, i);
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
                        if (lastWord == WordType.Number || lastWord == WordType.VarName)
                        {
                            lastWord = WordType.None;
                            AddBinaryOperation(layer, SymbolConvertor.multiplicationHandler, i);
                        }
                        if (lastWord == WordType.UnaryOp)
                        {
                            lastWord = WordType.None;
                            var oldOp = constructingOperations.Pop() as UnaryOperation;
                            SyntaxThrower.ThrowIfNull(oldOp, ErrorMessages.Unknown, i);
                            if (oldOp.operationHandler is NegateHandler)
                                constructingOperations.Push(oldOp);
                            else
                                constructingOperations.Push(new UnaryOperation(oldOp.operationHandler) { layer = oldOp.layer, order = newOrderForBracketedUnaryOp, startChar = oldOp.startChar });
                        }

                        layer++;
                        continue;
                    case ')':
                        SyntaxThrower.ThrowIf(layer == 0, ErrorMessages.TooManyBackets, i);

                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(text, i);
                        SyntaxThrower.ThrowIf(lastWord == WordType.None || lastWord == WordType.UnaryOp, ErrorMessages.InvalidOperation, i);

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

                        var handler = SymbolConvertor.SymbolToBinaryHandler(c);
                        if (handler == null)
                        {
                            if (constructingWord == ConstructingWordType.Number)
                                FinishConstructWord(text, i);
                            if (constructingWord == ConstructingWordType.None)
                                StartConstructWord(i, ConstructingWordType.Word);
                            continue;
                        }
                        if (constructingWord != ConstructingWordType.None)
                            FinishConstructWord(text, i);

                        lastWord = WordType.None;
                        AddBinaryOperation(layer, handler, i);
                        continue;
                }
            }

            if (constructingWord != ConstructingWordType.None)
                FinishConstructWord(text, text.Length);
            while (constructingOperations.TryPop(out var op) && TryFinishOp(op))
                ;

            SyntaxThrower.ThrowIf(finishedOperations.Count != 1, ErrorMessages.InvalidOperation, text.Length - 1);
            return finishedOperations.Pop();
        }

        private static bool TryFinishOp(BaseOperation op)
        {
            if (op is BinaryOperation biOp)
            {
                if (!finishedOperations.TryPop(out var subOp2) || !finishedOperations.TryPop(out var subOp1))
                    return false;

                SyntaxThrower.ThrowIf(subOp1.endChar > subOp2.startChar, ErrorMessages.Unknown, subOp2.startChar);

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
                    SyntaxThrower.ThrowIf(subOp.endChar > op.endChar, ErrorMessages.Unknown, op.startChar);
                    op.startChar = subOp.startChar;
                }
                else
                {
                    SyntaxThrower.ThrowIf(op.startChar > subOp.startChar, ErrorMessages.Unknown, subOp.startChar);
                    op.endChar = subOp.endChar;
                }
                uOp.inside = subOp;
            }

            finishedOperations.Push(op);
            return true;
        }
    }
}
