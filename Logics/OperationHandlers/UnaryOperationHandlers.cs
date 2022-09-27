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