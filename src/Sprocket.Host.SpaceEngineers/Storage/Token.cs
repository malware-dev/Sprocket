using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>One piece of script text, with whatever it carries.</summary>
    public struct Token
    {
        /// <summary>A piece of the sort <paramref name="kind" />, starting at <paramref name="at" />, spelled <paramref name="text" />, carrying <paramref name="whole" />, <paramref name="real" /> or <paramref name="keyword" /> where it is one of those.</summary>
        public Token(TokenKind kind, int at, StringSegment text = default(StringSegment), long whole = 0,
            double real = 0, Keyword keyword = Keyword.None) : this()
        {
            Kind = kind;
            At = at;
            Text = text;
            Whole = whole;
            Real = real;
            Keyword = keyword;
        }

        /// <summary>What sort of piece of text this is.</summary>
        public TokenKind Kind { get; private set; }

        /// <summary>
        /// Where in the script it starts.
        /// </summary>
        /// <remarks>
        /// A position rather than a line number, because a line number is only ever wanted when something
        /// has gone wrong, and counting lines as the text is read would pay for it on every token of every
        /// program that reads cleanly.
        /// </remarks>
        public int At { get; private set; }

        /// <summary>
        /// The text of an identifier, a string, an annotation, or what is wrong with a <see cref="TokenKind.Bad" />.
        /// </summary>
        /// <remarks>
        /// Where it lies in the script, not a copy of it. A copy is made only if something asks for one.
        /// </remarks>
        public StringSegment Text { get; private set; }

        /// <summary>An integer's value, or a colour's four bytes packed red-green-blue-alpha.</summary>
        public long Whole { get; private set; }

        /// <summary>A real number's value.</summary>
        public double Real { get; private set; }

        /// <summary>Which of the format's words this is, for a <see cref="TokenKind.Keyword" />.</summary>
        public Keyword Keyword { get; private set; }
    }
}
