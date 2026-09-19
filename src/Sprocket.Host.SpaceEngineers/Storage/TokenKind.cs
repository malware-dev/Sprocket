namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>What a piece of script text is, before anything knows what it means.</summary>
    enum TokenKind
    {
        /// <summary>Nothing left.</summary>
        End,

        /// <summary>Ends a statement. There are no continuations, so this is structural.</summary>
        Newline,

        /// <summary>
        /// A name: a bare word that is not one of the format's own, or anything in single quotes.
        /// </summary>
        /// <remarks>
        /// Single quotes are how a name holds what a bare word cannot - a space, a dot, a keyword - and a
        /// quoted name is never a keyword. It carries its text either way.
        /// </remarks>
        Identifier,

        /// <summary>A bare word that is one of the format's own. It carries no text at all.</summary>
        Keyword,

        /// <summary>
        /// Text, in double quotes.
        /// </summary>
        /// <remarks>
        /// Never a name. Once expressions exist a quote style cannot mean both - is <c>"Airlock"</c> a
        /// piece of text to compare, or the thing called that? - so text and names use different ones.
        /// </remarks>
        Text,

        Integer,

        Real,

        /// <summary>A whole number of ticks and its unit, written as one word: <c>600t</c>.</summary>
        Duration,

        /// <summary>Red, green, blue and alpha, packed in that order.</summary>
        Color,

        /// <summary>One line of text somebody typed, without its <c>#</c>.</summary>
        Annotation,

        BlockOpen,
        BlockClose,
        ListOpen,
        ListClose,
        VectorOpen,
        VectorClose,
        Comma,

        /// <summary>
        /// Joins the parts of a name: <c>MalTek.Airlock</c>.
        /// </summary>
        /// <remarks>
        /// A token of its own rather than a character inside a word, so spaces around it mean nothing and
        /// a name is always its parts. <c>'a.b'.'c'</c> and <c>'a'.'b.c'</c> are different names.
        /// </remarks>
        Dot,

        /// <summary>The <c>=</c> of one of a system saying what one of its parts is.</summary>
        Equals,

        /// <summary>
        /// Text that is not a token at all, carrying what is wrong with it.
        /// </summary>
        /// <remarks>
        /// A kind rather than an exception, because the script is the only copy of somebody's program:
        /// the reader has to be able to lose one line and keep the rest.
        /// </remarks>
        Bad
    }
}
