using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Every bare word the format gives a meaning to, and how to recognize one without making a string.
    /// </summary>
    /// <remarks>Persisted: every save spells these words this way.</remarks>
    public static class Keywords
    {
        static readonly string[] Names =
        {
            "",
            "sprocket", "block", "system", "instance", "of", "sequence",
            "when", "every", "then", "trigger", "to", "run", "set",
            "on", "off", "all", "any", "wait", "await", "required", "expects", "=", "changed", "as", "last",
            "running", "step", "stop", "call", "alpha", "optional", "let", "blueprint", "slot", "if", "else", "==",
            "signal", "sum", "!=", "<", ">", "<=", ">=", "whenever", "from", "average", "|", "type"
        };

        static readonly Keyword[][] ByLength = Bucket();

        /// <summary>How <paramref name="keyword" /> is spelled.</summary>
        public static string Name(Keyword keyword)
        {
            return Names[(int)keyword];
        }

        /// <summary>
        /// Which keyword a stretch of text spells, or <see cref="Keyword.None" />, reading it where it lies.
        /// </summary>
        /// <remarks>Allocates nothing.</remarks>
        public static Keyword Find(StringSegment word)
        {
            // By length, measured: one list for every bare word made the lexer 40% slower. A word with no keyword of its
            // length is turned away by one array read.
            if (word.Length >= ByLength.Length)
                return Keyword.None;

            var candidates = ByLength[word.Length];
            foreach (var candidate in candidates)
                if (word.Equals(Names[(int)candidate]))
                    return candidate;

            return Keyword.None;
        }

        /// <summary>Whether <paramref name="word" /> is spelled as a keyword, and so is quoted where it is a name.</summary>
        public static bool Taken(string word)
        {
            return !string.IsNullOrEmpty(word) && Find(new StringSegment(word)) != Keyword.None;
        }

        /// <summary>
        /// Whether a word can be written as it is, or has to be quoted.
        /// </summary>
        /// <remarks>Persisted: a bare word is a letter, then letters, digits and underscores, and no keyword.</remarks>
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
