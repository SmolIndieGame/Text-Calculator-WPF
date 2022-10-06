using System;

namespace Text_Caculator_WPF
{
    public abstract class BaseUnaryOperationHandler
    {
        public virtual bool isRightSide => false;
        public virtual int Order => 2;
        public abstract string Symbol { get; }
        public abstract OperationResult Calculate(double a);
    }

    [ParseInstantly]
    public sealed class NegateHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "-";
        public override OperationResult Calculate(double a) => -a;
    }

    [ParseInstantly]
    public sealed class PercentHandler : BaseUnaryOperationHandler
    {
        public override bool isRightSide => true;
        public override int Order => 1;
        public override string Symbol { get; } = "%";
        public override OperationResult Calculate(double a) => a * 0.01;
    }

    public sealed class DegreeHandler : BaseUnaryOperationHandler
    {
        public const double degreeToRad = Math.PI / 180;

        public override bool isRightSide => true;
        public override int Order => 1;
        public override string Symbol { get; } = "degree";
        public override OperationResult Calculate(double a) => a * degreeToRad;
    }

    public sealed class SineHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "sin";

        public override OperationResult Calculate(double a) => Math.Sin(a);
    }

    public sealed class CosineHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "cos";

        public override OperationResult Calculate(double a) => Math.Cos(a);
    }

    public sealed class TangentHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "tan";

        public override OperationResult Calculate(double a) => Math.Tan(a);
    }

    public sealed class LogarithmHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "log";

        public override OperationResult Calculate(double a) => Math.Log10(a);
    }

    public sealed class NaturalLogHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "ln";

        public override OperationResult Calculate(double a) => Math.Log(a);
    }

    public sealed class SquareRootHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "sqrt";

        public override OperationResult Calculate(double a) => Math.Sqrt(a);
    }

    public sealed class CubeRootHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "cbrt";

        public override OperationResult Calculate(double a) => Math.Cbrt(a);
    }

    [ParseInstantly]
    public sealed class FactorialHandler : BaseUnaryOperationHandler
    {
        public override bool isRightSide => true;
        public override int Order => 4;
        public override string Symbol { get; } = "!";

        public override OperationResult Calculate(double a)
        {
            if (Math.Floor(a) != a)
                return ErrorMessages.NotAInteger;
            if (a >= 171)
                return ErrorMessages.ResultTooLarge;

            double res = 1;
            for (int i = 2; i <= a; i++)
                res *= i;
            return res;
        }
    }
}