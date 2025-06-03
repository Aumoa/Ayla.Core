namespace Ayla.Core
{
    public static partial class ExpressionSerializer
    {
        private static string Quotes(this string value) => $"\"{value}\"";
    }
}
