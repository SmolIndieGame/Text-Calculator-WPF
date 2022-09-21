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

    public sealed class NegateHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "-";
        public override OperationResult Calculate(double a) => -a;
    }

    public sealed class SineHandler : BaseUnaryOperationHandler
    {
        public override string Symbol { get; } = "sin";

        public override OperationResult Calculate(double a) => Math.Sin(a);
    }

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