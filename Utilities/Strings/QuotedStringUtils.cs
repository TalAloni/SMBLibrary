using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public class QuotedStringUtils
    {
        public static string Quote(string str)
        {
            return String.Format("\"{0}\"", str);
        }

        public static string Unquote(string str)
        {
            string quote = '"'.ToString();
            if (str.Length >= 2 && str.StartsWith(quote) && str.EndsWith(quote))
            {
                return str.Substring(1, str.Length - 2);
            }
            else
            {
                return str;
            }
        }

        public static bool IsQuoted(string str)
        {
            string quote = '"'.ToString();
            if (str.Length >= 2 && str.StartsWith(quote) && str.EndsWith(quote))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static int IndexOfUnquotedChar(string str, char charToFind)
        {
            return IndexOfUnquotedChar(str, charToFind, 0);
        }

        public static int IndexOfUnquotedChar(string str, char charToFind, int startIndex)
        {
            if (startIndex >= str.Length)
            {
                return -1;
            }

            bool inQuote = false;
            int index = startIndex;
            while (index < str.Length)
            {
                if (str[index] == '"')
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote && str[index] == charToFind)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static int IndexOfUnquotedString(string str, string stringToFind)
        {
            return IndexOfUnquotedString(str, stringToFind, 0);
        }

        public static int IndexOfUnquotedString(string str, string stringToFind, int startIndex)
        {
            if (startIndex >= str.Length)
            {
                return -1;
            }

            bool inQuote = false;
            int index = startIndex;
            while (index < str.Length)
            {
                if (str[index] == '"')
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote && str.Substring(index).StartsWith(stringToFind))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static List<string> SplitIgnoreQuotedSeparators(string str, char separator)
        {
            return SplitIgnoreQuotedSeparators(str, separator, StringSplitOptions.None);
        }

        public static List<string> SplitIgnoreQuotedSeparators(string str, char separator, StringSplitOptions options)
        {
            List<string> result = new List<string>();
            int nextEntryIndex = 0;
            int separatorIndex = IndexOfUnquotedChar(str, separator);
            while (separatorIndex >= nextEntryIndex)
            {
                string entry = str.Substring(nextEntryIndex, separatorIndex - nextEntryIndex);
                if (options != StringSplitOptions.RemoveEmptyEntries || entry != String.Empty)
                {
                    result.Add(entry);
                }
                nextEntryIndex = separatorIndex + 1;
                separatorIndex = IndexOfUnquotedChar(str, separator, nextEntryIndex);
            }
            string lastEntry = str.Substring(nextEntryIndex);
            if (options != StringSplitOptions.RemoveEmptyEntries || lastEntry != String.Empty)
            {
                result.Add(lastEntry);
            }
            return result;
        }
    }
}
