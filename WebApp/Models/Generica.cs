#region

using System.Globalization;
using System.Text;

#endregion

namespace WebApp.Models
{
    public static class Generica
    {
        public static string RemoveAccents(this string text)
        {
            if (text == null)
                return string.Empty;
            var stringBuilder = new StringBuilder();
            foreach (var ch in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(ch);
            }
            return stringBuilder.ToString();
        }
    }
}