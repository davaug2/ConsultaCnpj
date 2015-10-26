#region

using System;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace WebApp.Models
{
    public static class StringHelper
    {
        public static bool CharCodeIsNumber(int charCode)
        {
            return charCode >= 48 && charCode <= 57;
        }

        public static string GetTextOnly(string text)
        {
            text = text.Replace("\t", "").Replace("\r", "").Replace("\n", "");

            var result = string.Empty;
            foreach (var item in text)
            {
                if (!CharCodeIsSpecialCharacter(item))
                    result += item;
            }

            return result;
        }

        public static bool CharCodeIsSpecialCharacter(int charCode)
        {
            return (charCode >= 33 && charCode <= 47 && charCode != 44) ||
                   (charCode >= 58 && charCode <= 64) ||
                   (charCode >= 91 && charCode <= 96);
        }

        public static string GetFirstName(string name)
        {
            return name.Split(' ')[0];
        }

        public static string GetLastName(string name)
        {
            var names = name.Split(' ');
            return names[names.Length - 1];
        }

        public static string GetRandomNumberString(int length)
        {
            var random = new Random();
            return
                random.Next(int.Parse("1".PadRight(length - 1, '0')), int.Parse("9".PadRight(length - 1, '9')))
                    .ToString();
        }

        public static string GetRandomString(int length)
        {
            var randomString = new StringBuilder();
            var random = new Random();
            while (randomString.Length < length)
            {
                var charCode = random.Next(97, 122);
                if (CharCodeIsSpecialCharacter(charCode)) continue;
                randomString.Append((char) charCode);
            }

            return randomString.ToString();
        }

        public static string GetRandom(int length)
        {
            var guid = Guid.NewGuid().ToString();
            guid = guid.Replace("-", "");
            while (guid.Length < length)
            {
                guid += Guid.NewGuid().ToString();
                guid = guid.Replace("-", "");
            }

            return "g" + guid.Substring(0, length - 1);
        }


        public static string GetStringNumbersOnly(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            return Regex.Replace(text, "[^0-9]", "");
        }

        public static long? GetNumbersOnly(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return null;

                var numbers = GetStringNumbersOnly(text);
                long longCast;
                return long.TryParse(numbers, out longCast) ? (long?) longCast : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static int GetNumbersOnlyToInt(string text)
        {
            try
            {
                var numbers = GetStringNumbersOnly(text);
                int intCast;
                return int.TryParse(numbers, out intCast) ? intCast : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string Truncate(string text, int size)
        {
            if (text == null)
                return text;

            if (text.Length > size)
                return text.Substring(0, size);

            return text;
        }
    }
}