using System.Globalization;
using System.Text;

namespace Aranea.HttpMessageHandler
{
    internal static class StringExtensions
    {
        internal static string FormatWith(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        internal static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
                return s;

            var sb = new StringBuilder();
            var lastIndex = s.Length - 1;
            for (var i = 0; i < s.Length; i++)
            {
                if (i == 0 || i == lastIndex || char.IsUpper(s[i + 1]))
                {
                    sb.Append(char.ToLower(s[i]));
                    continue;
                }
                sb.Append(s.Substring(i));
                break;
            }

            return sb.ToString();
        }
    }
}