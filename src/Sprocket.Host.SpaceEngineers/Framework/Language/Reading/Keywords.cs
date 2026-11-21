using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// Every bare word the format gives a meaning to, and how to recognize one without making a string: the reserved ones,
    /// a keyword wherever they stand, and the contextual ones, a keyword only where their construct reads them and a name
    /// anywhere else.
    /// </summary>
    /// <remarks>Persisted: every save spells these words this way.</remarks>
    public static class Keywords
    {
        /// <summary>The version of the format whose draft <see cref="ContextualDraft" /> is the first with contextual keywords.</summary>
        public const int ContextualVersion = 2;

        /// <summary>
        /// The draft of <see cref="ContextualVersion" /> that is the first with contextual keywords, and with <c>is</c>, <c>not</c>
        /// and <c>installed</c>. A program written for an earlier format reads every other keyword as a keyword wherever it
        /// stands, and those three as names.
        /// </summary>
        public const int ContextualDraft = 2;

        static readonly string[] Names =
        {
            "",
            "block", "system", "instance", "of", "sequence",
            "when", "every", "then", "to", "run", "set",
            "on", "off", "all", "any", "wait", "await", "required", "expects", "=", "changed", "as", "last",
            "stop", "call", "alpha", "optional", "let", "blueprint", "slot", "if", "else", "==",
            "signal", "sum", "!=", "<", ">", "<=", ">=", "whenever", "from", "average", "|", "type",
            "is", "not", "installed", "min", "max"
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

        /// <summary>Which keyword <paramref name="word" /> is spelled as, or <see cref="Keyword.None" />.</summary>
        public static Keyword Find(string word)
        {
            return string.IsNullOrEmpty(word) ? Keyword.None : Find(new StringSegment(word));
        }

        /// <summary>Whether <paramref name="keyword" /> is reserved, a keyword wherever it stands, rather than contextual.</summary>
        /// <remarks>Persisted: a name spelled like a reserved keyword is quoted in every save.</remarks>
        public static bool IsReserved(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.System:
                case Keyword.Blueprint:
                case Keyword.Instance:
                case Keyword.Sequence:
                case Keyword.Slot:
                case Keyword.Type:
                case Keyword.Let:
                case Keyword.Required:
                case Keyword.Optional:
                case Keyword.If:
                case Keyword.Else:
                case Keyword.Then:
                case Keyword.Call:
                case Keyword.Run:
                case Keyword.Stop:
                case Keyword.Set:
                case Keyword.Wait:
                case Keyword.Await:
                case Keyword.When:
                case Keyword.Whenever:
                case Keyword.Every:
                case Keyword.Is:
                case Keyword.Not:
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="keyword" /> arrived with <see cref="ContextualDraft" />, so a program written for an earlier format names things with it as with any other word: <c>is</c>, <c>not</c>, <c>installed</c>, <c>min</c> or <c>max</c>.</summary>
        public static bool ArrivedWithContextual(Keyword keyword)
        {
            return keyword == Keyword.Is || keyword == Keyword.Not || keyword == Keyword.Installed || keyword == Keyword.Min || keyword == Keyword.Max;
        }

        /// <summary>Whether <paramref name="word" /> is spelled as a reserved keyword, and so is quoted wherever it is a name.</summary>
        public static bool Reserved(string word)
        {
            return IsReserved(Find(word));
        }

        /// <summary>
        /// Whether a word can be written as it is, or has to be quoted.
        /// </summary>
        /// <remarks>
        /// Persisted: a bare word is a letter, then letters, digits and underscores, and no reserved keyword. A word spelled
        /// like a contextual keyword is quoted only where its construct reads that keyword, which the writer says there.
        /// </remarks>
        public static bool Bare(string word)
        {
            return Shaped(word) && !Reserved(word);
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
