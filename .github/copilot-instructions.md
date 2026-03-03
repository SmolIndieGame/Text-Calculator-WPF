# Copilot Instructions

## Repository Overview

Text-Calculator-WPF is a C# WPF desktop application targeting **.NET 6 (Windows)**. It provides a rich-text editor where users type mathematical expressions line by line and see results evaluated in real time. Files are saved/loaded in a custom `.txtc` format.

## Technology Stack

- **Language**: C# with nullable reference types enabled
- **UI Framework**: WPF (`UseWPF` is set in the project)
- **Target Framework**: `net6.0-windows`
- **Build**: `dotnet build` from the solution root

There are no automated tests in this repository. Validate changes by building the project.

## Project Structure

```
Text-Calculator-WPF/
├── App.xaml / App.xaml.cs          # Application entry point
├── MainWindow.xaml / .cs           # Main UI window and real-time evaluation loop
├── CommandHandling.cs              # File commands (New, Open, Save, SaveAs, Close)
├── AssemblyInfo.cs
└── Logics/
    ├── Evaluator.cs                # Top-level evaluation entry point (returns EvaluateResult)
    ├── SyntaxAnalyzer.cs           # Parses text into an operation tree (BaseOperation)
    ├── SyntaxThrower.cs            # Helpers to throw SyntaxException with position info
    ├── SymbolConvertor.cs          # Registry of all operators, constants, and user variables
    ├── ErrorMessages.cs            # Centralised error message strings
    ├── Operations/
    │   ├── BaseOperation.cs        # Abstract base; carries startChar/endChar/layer/order
    │   ├── BinaryOperation.cs      # Two-operand operation (left, right children)
    │   ├── UnaryOperation.cs       # Single-operand operation (inside child)
    │   └── LiteralOperation.cs     # Leaf node holding a numeric value
    └── OperationHandlers/
        ├── BinaryOperationHandlers.cs  # +, *, /, ^, mod
        ├── UnaryOperationHandlers.cs   # -, %, !, sin, cos, tan, log, ln, sqrt, cbrt, degree
        ├── OperationResult.cs          # Result type returned by Calculate()
        └── ParseInstantlyAttribute.cs  # Marks single-character operators for fast lookup
```

## Architecture: Adding a New Operation

1. **Create a handler class** in `Logics/OperationHandlers/BinaryOperationHandlers.cs` (binary) or `UnaryOperationHandlers.cs` (unary) by subclassing `BaseBinaryOperationHandler` or `BaseUnaryOperationHandler`.
   - Override `Symbol` (the text/character used in expressions).
   - Override `Order` (operator precedence; lower = evaluated first / lower binding).
   - Override `Calculate(...)` to return an `OperationResult` (implicit from `double`, or an error string from `ErrorMessages`).
2. **Single-character symbols only**: apply `[ParseInstantly]` — the symbol **must** be exactly one character long.
3. **Multi-character symbols** (e.g. `"mod"`, `"sqrt"`): do **not** apply `[ParseInstantly]`; `SymbolConvertor` discovers them automatically via reflection at startup.
4. `SymbolConvertor` discovers all concrete subclasses via reflection on startup — no registration step is required.

## Namespaces

- UI code lives in namespace `Text_Calculator_WPF`.
- Logic/engine code lives in namespace `Text_Caculator_WPF` (note the typo — preserve it for consistency).

## Code Conventions

- Use `ReadOnlySpan<char>` for performance-sensitive text parsing.
- Error positions are tracked as `(startIndex, endIndex)` character offsets within the current line.
- `SyntaxThrower.ThrowIf` is the idiomatic way to emit a `SyntaxException`.
- `OperationResult` supports implicit conversion from `double` and from `string` (error message).
- Keep operator `Order` values consistent with existing operators: `0` = additive, `1` = multiplicative/modulo, `2` = default unary, `3` = exponentiation, `4`+ = high-precedence postfix.
