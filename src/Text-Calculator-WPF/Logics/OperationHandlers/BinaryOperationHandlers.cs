using System;

namespace Text_Caculator_WPF
{
    public abstract class BaseBinaryOperationHandler
    {
        public virtual bool leftToRight => true;
        public abstract int Order { get; }
        public abstract string Symbol { get; }
        public abstract OperationResult Calculate(double a, double b);
    }

    [ParseInstantly]
    public sealed class AdditionHandler : BaseBinaryOperationHandler
    {
        public override string Symbol { get; } = "+";
        public override int Order => 0;
        public override OperationResult Calculate(double a, double b) => a + b;
    }

    [ParseInstantly]
    public sealed class MultiplicationHandler : BaseBinaryOperationHandler
    {
        public override string Symbol { get; } = "*";
        public override int Order => 1;
        public override OperationResult Calculate(double a, double b) => a * b;
    }

    [ParseInstantly]
    public sealed class DivisionHandler : BaseBinaryOperationHandler
    {
        public override string Symbol { get; } = "/";
        public override int Order => 1;
        public override OperationResult Calculate(double a, double b)
        {
            if (b == 0)
                return ErrorMessages.DividedByZero;
            return a / b;
        }
    }

    public sealed class ModuloHandler : BaseBinaryOperationHandler
    {
        public override string Symbol { get; } = "mod";
        public override int Order => 1;
        public override OperationResult Calculate(double a, double b)
        {
            if (b == 0)
                return ErrorMessages.DividedByZero;
            return a % b;
        }
    }

    [ParseInstantly]
    public sealed class PowerHandler : BaseBinaryOperationHandler
    {
        public override bool leftToRight => false;
        public override string Symbol { get; } = "^";
        public override int Order => 3;
        public override OperationResult Calculate(double a, double b) => Math.Pow(a, b);
    }
}