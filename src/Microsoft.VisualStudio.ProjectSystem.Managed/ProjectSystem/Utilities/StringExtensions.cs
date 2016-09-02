namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    using System.Text;

    /// <summary>
    /// Extensions to the string class.
    ///</summary>
    internal static class StringExtensions
    {

        /// <summary>
        /// Adds leading and trailing quotes. If already quoted, does nothing.
        /// </summary>
        public static string QuoteString(this string input)
        {
            var sb = new StringBuilder();
            if (!input.StartsWith("\""))
            {
                sb.Append("\"");
                sb.Append(input);
            }
            else
            {
                sb.Append(input);
            }

            if (!input.EndsWith("\""))
            {
                sb.Append("\"");
            }

            return sb.ToString();
        }
    }
}

