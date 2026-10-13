using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sprocket.Localization.Generator
{
    /// <summary>What a text and its comment say to somebody who reads the one text alone: a reviewer, a translator, a tool.</summary>
    static class Notes
    {
        /// <summary>Words that point at another text in the file rather than at the screen.</summary>
        static readonly Regex Pointing = new Regex(
            @"\b(above|below|the next one|the next text|the previous one|the one before|as before|same as the|the other one|ditto)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static readonly Regex Placeholder = new Regex(@"\{[A-Za-z][A-Za-z0-9]*\}", RegexOptions.CultureInvariant);

        /// <summary>A sentence ending, then more words: the mark, any closing marks, and a space before something.</summary>
        static readonly Regex Ending = new Regex(@"[.!?][""')\]*]*\s+\S", RegexOptions.CultureInvariant);

        /// <summary>The words in <paramref name="comment" /> that point at another text, or null where none do.</summary>
        public static string Leaning(string comment)
        {
            var found = Pointing.Match(comment);
            return found.Success ? found.Value : null;
        }

        /// <summary>Each placeholder in <paramref name="text" />, such as <c>{name}</c>, once.</summary>
        public static List<string> Placeholders(string text)
        {
            var found = new List<string>();
            foreach (Match match in Placeholder.Matches(text))
            {
                if (!found.Contains(match.Value))
                    found.Add(match.Value);
            }

            return found;
        }

        /// <summary>Whether <paramref name="text" /> is several sentences and its last ends without a period.</summary>
        /// <remarks>
        /// A single line is left alone: whether it is a sentence or a fragment, a label or a short command, is read, not
        /// counted. A last sentence may end in a question mark, an exclamation mark, a colon or an ellipsis, and closing
        /// marks after it are looked past.
        /// </remarks>
        public static bool Unfinished(string text)
        {
            var trimmed = text.Trim();
            if (!Ending.IsMatch(trimmed))
                return false;

            var end = trimmed.TrimEnd('*', ')', ']', '"', '\'', ' ');
            if (end.Length == 0)
                return false;

            var last = end[end.Length - 1];
            return last != '.' && last != '?' && last != '!' && last != ':' && last != '…';
        }
    }
}
