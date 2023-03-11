using System.Globalization;
using System.Text;

namespace Stipps.CloudflareApi;

public static class ExtensionsMethods
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var sb = new StringBuilder();
        var lastChar = str[0];
        sb.Append(char.ToLower(lastChar, CultureInfo.InvariantCulture));

        for (var i = 1; i < str.Length; i++)
        {
            var currentChar = str[i];
            if (char.IsUpper(currentChar) && !char.IsUpper(lastChar))
            {
                sb.Append('_');
            }

            sb.Append(char.ToLower(currentChar, CultureInfo.InvariantCulture));
            lastChar = currentChar;
        }

        return sb.ToString();
    }
}