namespace Reactor.Storage
{
    /// <summary>
    /// Every bare word the format gives a meaning to.
    /// </summary>
    /// <remarks>
    /// <b>Nothing is reserved.</b> A host may well name an action <c>open</c>, and one may name it
    /// <c>every</c>; the format keeps both by quoting the second, because a quoted word is never a
    /// keyword. So this list is what the writer has to quote around, and the reader never consults it -
    /// it reads a bare word where it expects one and data where it expects data.
    /// <para>
    /// <c>run</c> and <c>set</c> are here before anything writes them, deliberately. Quoting an action
    /// that collides with a word the language is going to grow costs nothing today and means a save
    /// written now still reads correctly when it does.
    /// </para>
    /// </remarks>
    static class Keywords
    {
        static readonly string[] All =
        {
            "reactor", "block", "system", "role", "instance", "fill", "sequence", "step",
            "when", "every", "called", "after", "once", "onlyif", "run", "set",
            "on", "off", "all", "any", "wait", "now", "optional", "required"
        };

        public static bool Taken(string word)
        {
            for (var i = 0; i < All.Length; i++)
                if (All[i] == word)
                    return true;

            return false;
        }

        /// <summary>
        /// Whether a word can be written as it is, or has to be quoted.
        /// </summary>
        /// <remarks>
        /// The same shape the reader accepts bare: a letter, then letters, digits, underscores and
        /// dashes. Everything else is quoted mechanically, which covers the game's own oddities -
        /// properties called <c>Blink Interval</c>, subtypes with spaces - without anybody choosing.
        /// </remarks>
        public static bool Bare(string word)
        {
            if (string.IsNullOrEmpty(word) || Taken(word))
                return false;

            var first = word[0];
            if (!Letter(first))
                return false;

            for (var i = 1; i < word.Length; i++)
            {
                var c = word[i];
                if (!Letter(c) && (c < '0' || c > '9') && c != '_' && c != '-')
                    return false;
            }

            return true;
        }

        static bool Letter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
    }
}
