using System;
using System.Collections.Generic;
using System.Linq;

namespace Text_Calculator_WPF;

public static class SymbolConverter
{
    static readonly List<BaseBinaryOperationHandler> _largeBinaryOperationHandlers;
    static readonly List<BaseBinaryOperationHandler> _smallBinaryOperationHandlers;

    static readonly List<BaseUnaryOperationHandler> _largeUnaryOperationHandlers;
    static readonly List<BaseUnaryOperationHandler> _smallUnaryOperationHandlers;

    static readonly BaseBinaryOperationHandler _additionHandler;
    static readonly BaseBinaryOperationHandler _multiplicationHandler;

    static readonly string[] _keywords;
    static readonly List<(string name, double value)> _constants;
    static readonly List<(string name, double value)> _userVariables;
    static int _currentLineNumber;

    static SymbolConverter()
    {
        _largeBinaryOperationHandlers = new List<BaseBinaryOperationHandler>();
        _largeUnaryOperationHandlers = new List<BaseUnaryOperationHandler>();
        _smallBinaryOperationHandlers = new List<BaseBinaryOperationHandler>();
        _smallUnaryOperationHandlers = new List<BaseUnaryOperationHandler>();

        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assem => assem.GetTypes()).ToArray();
        foreach (var type in allTypes)
        {
            if (type.IsAbstract) continue;

            if (typeof(BaseBinaryOperationHandler).IsAssignableFrom(type))
            {
                if (Activator.CreateInstance(type) is not BaseBinaryOperationHandler obj)
                    continue;

                if (type == typeof(AdditionHandler)) _additionHandler = obj;
                if (type == typeof(MultiplicationHandler)) _multiplicationHandler = obj;

                if (type.IsDefined(typeof(ParseInstantlyAttribute), false))
                {
                    if (obj.Symbol.Length != 1)
                        throw new Exception($"The symbol length of {type} should be 1 for it to be instantly parsable.");
                    _smallBinaryOperationHandlers.Add(obj);
                }
                else
                    _largeBinaryOperationHandlers.Add(obj);
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
                    _smallUnaryOperationHandlers.Add(obj);
                }
                else
                    _largeUnaryOperationHandlers.Add(obj);
            }
        }

        if (_additionHandler == null || _multiplicationHandler == null)
            throw new Exception("'+' or '*' handler does not exists.");

        _keywords =
        [
            "Dim",
            "dim"
        ];
        _constants =
        [
            ("pi", Math.PI),
            ("e", Math.E),
            ("tau", Math.Tau)
        ];
        _userVariables = [];
    }

    public static BaseBinaryOperationHandler GetMulConnector() => _multiplicationHandler;
    public static BaseBinaryOperationHandler GetConnectorFor(BaseUnaryOperationHandler handler)
    {
        if (handler is NegateHandler)
            return _additionHandler;
        return _multiplicationHandler;
    }

    public static BaseBinaryOperationHandler? NameToBinaryHandler(ReadOnlySpan<char> name)
    {
        for (int i = 0; i < _largeBinaryOperationHandlers.Count; i++)
            if (_largeBinaryOperationHandlers[i].Symbol.AsSpan().SequenceEqual(name))
                return _largeBinaryOperationHandlers[i];
        return null;
    }
    public static BaseUnaryOperationHandler? NameToUnaryHandler(ReadOnlySpan<char> name)
    {
        for (int i = 0; i < _largeUnaryOperationHandlers.Count; i++)
            if (_largeUnaryOperationHandlers[i].Symbol.AsSpan().SequenceEqual(name))
                return _largeUnaryOperationHandlers[i];
        return null;
    }

    public static BaseBinaryOperationHandler? SymbolToBinaryHandler(char c)
    {
        for (int i = 0; i < _smallBinaryOperationHandlers.Count; i++)
            if (_smallBinaryOperationHandlers[i].Symbol[0] == c)
                return _smallBinaryOperationHandlers[i];
        return null;
    }
    public static BaseUnaryOperationHandler? SymbolToUnaryHandler(char c)
    {
        for (int i = 0; i < _smallUnaryOperationHandlers.Count; i++)
            if (_smallUnaryOperationHandlers[i].Symbol[0] == c)
                return _smallUnaryOperationHandlers[i];
        return null;
    }

    public static double NameToConstant(ReadOnlySpan<char> chars)
    {
        for (int i = 0; i < _constants.Count; i++)
            if (_constants[i].name.AsSpan().SequenceEqual(chars))
                return _constants[i].value;
        return double.NaN;
    }

    public static double NameToUserVariable(ReadOnlySpan<char> chars)
    {
        if (_currentLineNumber >= _userVariables.Count)
            return double.NaN;
        for (int i = _currentLineNumber - 1; i >= 0; i--)
            if (_userVariables[i].name.AsSpan().SequenceEqual(chars))
                return _userVariables[i].value;
        return double.NaN;
    }

    public static bool IsIdentifierPreserved(ReadOnlySpan<char> name)
    {
        if (NameToUnaryHandler(name) != null || NameToBinaryHandler(name) != null)
            return true;

        for (int i = 0; i < _keywords.Length; i++)
            if (_keywords[i].AsSpan().SequenceEqual(name))
                return true;

        for (int i = 0; i < _constants.Count; i++)
            if (_constants[i].name.AsSpan().SequenceEqual(name))
                return true;
        return false;
    }

    public static void SetLineNumber(int lineNumber) => _currentLineNumber = lineNumber;

    public static void SetUserVariable(string name, double value)
    {
        while (_currentLineNumber >= _userVariables.Count)
            _userVariables.Add((string.Empty, double.NaN));

        _userVariables[_currentLineNumber] = (name, value);
    }
}
