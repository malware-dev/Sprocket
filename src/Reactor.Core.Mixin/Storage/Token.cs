namespace Reactor.Storage
{
    /// <summary>One piece of script text, with whatever it carries.</summary>
    struct Token
    {
        public Token(TokenKind kind, int line, string word = null, long whole = 0, double real = 0)
            : this()
        {
            Kind = kind;
            Line = line;
            Word = word ?? "";
            Whole = whole;
            Real = real;
        }

        public TokenKind Kind { get; private set; }

        /// <summary>Which line it was on, so trouble can say where.</summary>
        public int Line { get; private set; }

        /// <summary>The text of an identifier, a string, an annotation, or what is wrong with a <see cref="TokenKind.Bad" />.</summary>
        public string Word { get; private set; }

        /// <summary>An integer's value, or a colour's four bytes packed red-green-blue-alpha.</summary>
        public long Whole { get; private set; }

        public double Real { get; private set; }
    }
}
