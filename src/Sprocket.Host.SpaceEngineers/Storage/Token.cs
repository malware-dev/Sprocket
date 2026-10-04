using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>One piece of script text, with whatever it carries.</summary>
    public struct Token
    {
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

        public double Real { get; private set; }

        /// <summary>Which of the format's words this is, for a <see cref="TokenKind.Keyword" />.</summary>
        public Keyword Keyword { get; private set; }
    }
}
