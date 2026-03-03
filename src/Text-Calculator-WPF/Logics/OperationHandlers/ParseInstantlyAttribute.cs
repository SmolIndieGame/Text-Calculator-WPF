using System;

namespace Text_Caculator_WPF
{
    /// <summary>
    /// Mark this OperationHandler as instantly parsable (require the symbol to be 1 character long),<br/>
    /// making it possible to chain operators.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ParseInstantlyAttribute : Attribute { }
}