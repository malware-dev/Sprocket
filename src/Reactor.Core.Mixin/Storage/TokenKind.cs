namespace Reactor.Storage
{
    /// <summary>What a piece of script text is, before anything knows what it means.</summary>
    enum TokenKind
    {
        /// <summary>Nothing left.</summary>
        End,

        /// <summary>Ends a statement. There are no continuations, so this is structural.</summary>
        Newline,

        /// <summary>A bare word: a keyword, or an id that needed no quoting.</summary>
        Identifier,

        /// <summary>A quoted string. Legal anywhere an identifier is, and never a keyword.</summary>
        Text,

        Integer,

        Real,

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
        /// Text that is not a token at all, carrying what is wrong with it.
        /// </summary>
        /// <remarks>
        /// A kind rather than an exception, because the script is the only copy of somebody's program:
        /// the reader has to be able to lose one line and keep the rest.
        /// </remarks>
        Bad
    }
}
