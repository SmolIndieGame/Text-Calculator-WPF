using System;
using System.Collections.Generic;
using System.Linq;

namespace Text_Caculator_WPF
{
    public static class SymbolConvertor
    {
        static char[] binaryOperationHandlerNames;
        static BaseBinaryOperationHandler[] binaryOperationHandlers;

        static string[] unaryOperationHandlerNames;
        static BaseUnaryOperationHandler[] unaryOperationHandlers;

        public static AdditionHandler additionHandler { get; }
        public static MultiplicationHandler multiplicationHandler { get; }

        static SymbolConvertor()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assem => assem.GetTypes()).ToArray();

            var binaryOperationHandlersList = new List<(char symbol, BaseBinaryOperationHandler handler)>();
            var unaryOperationHandlersList = new List<(string symbol, BaseUnaryOperationHandler handler)>();
            foreach (var type in allTypes)
            {
                if (type.IsAbstract) continue;

                if (typeof(BaseBinaryOperationHandler).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is not BaseBinaryOperationHandler obj)
                        continue;

                    binaryOperationHandlersList.Add((obj.Symbol, obj));
                }

                if (typeof(BaseUnaryOperationHandler).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is not BaseUnaryOperationHandler obj)
                        continue;

                    unaryOperationHandlersList.Add((obj.Symbol, obj));
                }
            }

            binaryOperationHandlerNames = new char[binaryOperationHandlersList.Count];
            binaryOperationHandlers = new BaseBinaryOperationHandler[binaryOperationHandlersList.Count];
            for (int i = 0; i < binaryOperationHandlersList.Count; i++)
            {
                binaryOperationHandlerNames[i] = binaryOperationHandlersList[i].symbol;
                binaryOperationHandlers[i] = binaryOperationHandlersList[i].handler;

                if (binaryOperationHandlers[i] is AdditionHandler addHandler)
                    additionHandler = addHandler;
                if (binaryOperationHandlers[i] is MultiplicationHandler mulHandler)
                    multiplicationHandler = mulHandler;
            }

            unaryOperationHandlerNames = new string[unaryOperationHandlersList.Count];
            unaryOperationHandlers = new BaseUnaryOperationHandler[unaryOperationHandlersList.Count];
            for (int i = 0; i < unaryOperationHandlersList.Count; i++)
            {
                unaryOperationHandlerNames[i] = unaryOperationHandlersList[i].symbol;
                unaryOperationHandlers[i] = unaryOperationHandlersList[i].handler;
            }

            if (additionHandler == null || multiplicationHandler == null)
                throw new Exception("'+' or '*' handler does not exists.");
        }

        public static BaseBinaryOperationHandler? SymbolToBinaryHandler(char c)
        {
            for (int i = 0; i < binaryOperationHandlerNames.Length; i++)
                if (binaryOperationHandlerNames[i] == c)
                    return binaryOperationHandlers[i];
            return null;
        }

        public static BaseUnaryOperationHandler? SymbolToUnaryHandler(ReadOnlySpan<char> chars)
        {
            for (int i = 0; i < unaryOperationHandlers.Length; i++)
                if (unaryOperationHandlerNames[i].AsSpan().SequenceEqual(chars))
                    return unaryOperationHandlers[i];
            return null;
        }
    }
}
