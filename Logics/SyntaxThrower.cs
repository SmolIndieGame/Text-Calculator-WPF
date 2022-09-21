using System.Diagnostics.CodeAnalysis;

namespace Text_Caculator_WPF
{
    internal static class SyntaxThrower
    {
        public static void ThrowIf(bool condition, string message, int index)
        {
            if (condition)
                throw new SyntaxException(message, index, index + 1);
        }

        public static void ThrowIf(bool condition, string message, int start, int end)
        {
            if (condition)
                throw new SyntaxException(message, start, end);
        }

        public static void ThrowIfNull<T>([NotNull] T? obj, string message, int index) where T : class
        {
            if (obj is null)
                throw new SyntaxException(message, index, index + 1);
        }

        public static void ThrowIfNull<T>([NotNull] T? obj, string message, int start, int end) where T : class
        {
            if (obj is null)
                throw new SyntaxException(message, start, end);
        }
    }
}