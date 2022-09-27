using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Text_Caculator_WPF
{
    public static class SymbolConvertor
    {
        static readonly List<BaseBinaryOperationHandler> largeBinaryOperationHandlers;
        static readonly List<BaseBinaryOperationHandler> smallBinaryOperationHandlers;

        static readonly List<BaseUnaryOperationHandler> largeUnaryOperationHandlers;
        static readonly List<BaseUnaryOperationHandler> smallUnaryOperationHandlers;

        static readonly BaseBinaryOperationHandler additionHandler;
        static readonly BaseBinaryOperationHandler multiplicationHandler;

        static readonly string[] keywords;
        static readonly List<(string name, double value)> constants;
        static readonly List<(string name, double value)> userVariables;
        static int currentLineNumber;

        static SymbolConvertor()
        {
            largeBinaryOperationHandlers = new List<BaseBinaryOperationHandler>();
            largeUnaryOperationHandlers = new List<BaseUnaryOperationHandler>();
            smallBinaryOperationHandlers = new List<BaseBinaryOperationHandler>();
            smallUnaryOperationHandlers = new List<BaseUnaryOperationHandler>();

            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assem => assem.GetTypes()).ToArray();
            foreach (var type in allTypes)
            {
                if (type.IsAbstract) continue;

                if (typeof(BaseBinaryOperationHandler).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is not BaseBinaryOperationHandler obj)
                        continue;

                    if (type == typeof(AdditionHandler)) additionHandler = obj;
                    if (type == typeof(MultiplicationHandler)) multiplicationHandler = obj;

                    if (type.IsDefined(typeof(ParseInstantlyAttribute), false))
                    {
                        if (obj.Symbol.Length != 1)
                            throw new Exception($"The symbol length of {type} should be 1 for it to be instantly parsable.");
                        smallBinaryOperationHandlers.Add(obj);
                    }
                    else
                        largeBinaryOperationHandlers.Add(obj);
                    continue;
                }

                if (typeof(BaseUnaryOperationHandler).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is not BaseUnaryOperationHandler obj)
                        continue;

                    if (type.IsDefined(typeof(ParseInstantlyAttribute), false))
                    {
                        if (obj.Symbol.Length != 1)
                            throw new Exception($"The symbol length of {type} should be 1 for it to be instantly parsable.");
                        smallUnaryOperationHandlers.Add(obj);
                    }
                    else
                        largeUnaryOperationHandlers.Add(obj);
                }
            }

            if (additionHandler == null || multiplicationHandler == null)
                throw new Exception("'+' or '*' handler does not exists.");

            keywords = new string[]
            {
                "Dim",
                "dim"
            };
            constants = new()
            {
                ("pi", Math.PI),
                ("e", Math.E),
                ("tau", Math.Tau)
            };
            userVariables = new();
        }

        public static BaseBinaryOperationHandler GetMulConnector() => multiplicationHandler;
        public static BaseBinaryOperationHandler GetConnectorFor(BaseUnaryOperationHandler handler)
        {
            if (handler is NegateHandler)
                return additionHandler;
            return multiplicationHandler;
        }

        public static BaseBinaryOperationHandler? NameToBinaryHandler(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < largeBinaryOperationHandlers.Count; i++)
                if (largeBinaryOperationHandlers[i].Symbol.AsSpan().SequenceEqual(name))
                    return largeBinaryOperationHandlers[i];
            return null;
        }
        public static BaseUnaryOperationHandler? NameToUnaryHandler(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < largeUnaryOperationHandlers.Count; i++)
                if (largeUnaryOperationHandlers[i].Symbol.AsSpan().SequenceEqual(name))
                    return largeUnaryOperationHandlers[i];
            return null;
        }

        public static BaseBinaryOperationHandler? SymbolToBinaryHandler(char c)
        {
            for (int i = 0; i < smallBinaryOperationHandlers.Count; i++)
                if (smallBinaryOperationHandlers[i].Symbol[0] == c)
                    return smallBinaryOperationHandlers[i];
            return null;
        }
        public static BaseUnaryOperationHandler? SymbolToUnaryHandler(char c)
        {
            for (int i = 0; i < smallUnaryOperationHandlers.Count; i++)
                if (smallUnaryOperationHandlers[i].Symbol[0] == c)
                    return smallUnaryOperationHandlers[i];
            return null;
        }

        public static double NameToConstant(ReadOnlySpan<char> chars)
        {
            for (int i = 0; i < constants.Count; i++)
                if (constants[i].name.AsSpan().SequenceEqual(chars))
                    return constants[i].value;
            return double.NaN;
        }

        public static double NameToUserVariable(ReadOnlySpan<char> chars)
        {
            if (currentLineNumber >= userVariables.Count)
                return double.NaN;
            for (int i = currentLineNumber - 1; i >= 0; i--)
                if (userVariables[i].name.AsSpan().SequenceEqual(chars))
                    return userVariables[i].value;
            return double.NaN;
        }

        public static bool IsIdentifierPreserved(ReadOnlySpan<char> name)
        {
            if (NameToUnaryHandler(name) != null || NameToBinaryHandler(name) != null)
                return true;

            for (int i = 0; i < keywords.Length; i++)
                if (keywords[i].AsSpan().SequenceEqual(name))
                    return true;

            for (int i = 0; i < constants.Count; i++)
                if (constants[i].name.AsSpan().SequenceEqual(name))
                    return true;
            return false;
        }

        public static void SetLineNumber(int lineNumber) => currentLineNumber = lineNumber;

        /*public static bool AddUserConstant(string name, double value)
        {
            if (SymbolToUnaryHandler(name) != null)
                return false;

            for (int i = 0; i < userVariables.Count; i++)
                if (userVariables[i].name.AsSpan().SequenceEqual(name))
                {
                    userVariables[i] = (name, value);
                    return true;
                }
            userVariables.Add((name, value));
            return true;
        }

        public static bool RemoveUserConstant(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < userVariables.Count; i++)
                if (userVariables[i].name.AsSpan().SequenceEqual(name))
                {
                    userVariables.RemoveAt(i);
                    return true;
                }
            return false;
        }*/

        public static void SetUserVariable(string name, double value)
        {
            while (currentLineNumber >= userVariables.Count)
                userVariables.Add((string.Empty, double.NaN));

            userVariables[currentLineNumber] = (name, value);
        }
    }
}
