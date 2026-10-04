using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Every bare word the format gives a meaning to, and how to recognise one without making a string.
    /// </summary>
    /// <remarks>
    /// <b>Nothing is reserved.</b> A host may well name an action <c>open</c>, and one may name it
    /// <c>every</c>; the format keeps both by quoting the second, because a quoted word is never a
    /// keyword. So the writer quotes around this list, and the reader only asks it about bare words.
    /// <para>
    /// These strings are persisted: every save ever written spells them this way, so changing one is a
    /// change to what every existing file means.
    /// </para>
    /// </remarks>
    static class Keywords
    {
        static readonly string[] Names =
        {
            "",
            "sprocket", "block", "system", "instance", "of", "sequence",
            "when", "every", "then", "trigger", "to", "run", "set",
            "on", "off", "all", "any", "wait", "await", "required", "expects", "=", "changed", "as", "last",
            "running", "step", "stop", "call", "with", "alpha", "optional", "let", "blueprint", "slot", "if", "else", "=="
        };

        static readonly Keyword[][] ByLength = Bucket();

        public static string Name(Keyword keyword)
        {
            return Names[(int)keyword];
        }

        /// <summary>
        /// Which keyword a stretch of text spells, or <see cref="Keyword.None" />, reading it where it lies.
        /// </summary>
        /// <remarks>
        /// On a segment rather than a substring, because making the substring is the cost being avoided: a
        /// save is mostly these words, and each one would otherwise be an allocation thrown away as soon
        /// as it had been compared.
        /// </remarks>
        public static Keyword Find(StringSegment word)
        {
            // By length, measured: looking every bare word up in one list made the lexer 40% slower, because most
            // bare words are no keyword at all. A name like entity123 has no keyword of its length and is turned
            // away by one array read.
            if (word.Length >= ByLength.Length)
                return Keyword.None;

            var candidates = ByLength[word.Length];
            for (var i = 0; i < candidates.Length; i++)
                if (word.Equals(Names[(int)candidates[i]]))
                    return candidates[i];

            return Keyword.None;
        }

        public static bool Taken(string word)
        {
            return !string.IsNullOrEmpty(word) && Find(new StringSegment(word)) != Keyword.None;
        }

        /// <summary>
        /// Whether a word can be written as it is, or has to be quoted.
        /// </summary>
        /// <remarks>
        /// The same shape the reader accepts bare: a letter, then letters, digits and underscores.
        /// Everything else is quoted mechanically, which covers the game's own oddities - properties called
        /// <c>Blink Interval</c>, subtypes with spaces - without anybody choosing.
        /// <para>
        /// <b>A dash is not one of them, and that is about subtraction.</b> Were it a name character,
        /// <c>a-1</c> would be one name and <c>a - 1</c> a subtraction, so the same text would mean two
        /// different things depending on a space and the wrong reading would be the silent one.
        /// </para>
        /// </remarks>
        public static bool Bare(string word)
        {
            return Shaped(word) && !Taken(word);
        }

        /// <summary>Whether a word has the shape of a name - a letter, then letters, digits and underscores - keyword or not.</summary>
        static bool Shaped(string word)
        {
            if (string.IsNullOrEmpty(word) || !Letter(word[0]))
                return false;

            for (var i = 1; i < word.Length; i++)
            {
                var c = word[i];
                if (!Letter(c) && (c < '0' || c > '9') && c != '_')
                    return false;
            }

            return true;
        }

        static bool Letter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        static Keyword[][] Bucket()
        {
            var longest = 0;
            for (var i = 1; i < Names.Length; i++)
                if (Names[i].Length > longest)
                    longest = Names[i].Length;

            var counts = new int[longest + 1];
            for (var i = 1; i < Names.Length; i++)
                counts[Names[i].Length]++;

            var buckets = new Keyword[longest + 1][];
            for (var length = 0; length <= longest; length++)
                buckets[length] = new Keyword[counts[length]];

            var filled = new int[longest + 1];
            for (var i = 1; i < Names.Length; i++)
            {
                var length = Names[i].Length;
                buckets[length][filled[length]++] = (Keyword)i;
            }

            return buckets;
        }
    }
}
