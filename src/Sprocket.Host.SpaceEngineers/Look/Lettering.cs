namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>Text sizes, as the height of a line in UI units, named for what they are for.</summary>
    /// <remarks>
    ///     Exact conversions of scales measured in the game: a line of the game's text is 37 units at a scale of
    ///     one, and a line of the symbol font 64.
    /// </remarks>
    public static class Lettering
    {
        /// <summary>A screen's name.</summary>
        public const float Title = 33.3f;

        /// <summary>Everything that is a sentence, a row or a label.</summary>
        /// <remarks>
        ///     Measured in the game: the terminal's list rows, section headings, field contents, button
        ///     captions and window title all have a cap height of fourteen units, which this size lands on.
        /// </remarks>
        public const float Body = 23.68f;

        /// <summary>A step down, for a control rather than a sentence.</summary>
        /// <remarks>
        ///     Measured in the game: the terminal's tabs and top buttons have a cap height of about twelve and
        ///     a half units, the smallest text it draws.
        /// </remarks>
        public const float Small = 21.09f;

        /// <summary>The quietest thing that still carries a word: a count, a "ran 4s ago", a kind under a name.</summary>
        public const float Tiny = 18.5f;

        /// <summary>A symbol standing beside a line of text.</summary>
        public const float Symbol = 36.48f;

        /// <summary>The pencil beside a name that can be changed, a step above an ordinary symbol.</summary>
        public const float Pen = 40.96f;

        /// <summary>How far to lift a symbol that stands on a line of the game's text, as a fraction of it.</summary>
        /// <remarks>
        ///     The game sets its letters slightly high in their line, so a symbol centred in its own sits low
        ///     beside them. Measured in the game: a symbol beside the heading sat nine pixels below the middle
        ///     of its capitals on a 3840-wide screen, four and a half units. Only the game can check this; the
        ///     simulator places glyphs by its own rule, and a recorded frame cannot see it.
        ///     <para>
        ///         Not for a symbol centered in a row of its own, which lands on the row's middle unaided, measured to
        ///         within two pixels.
        ///     </para>
        /// </remarks>
        public const float SymbolLift = 0.123f;

        /// <summary>The fixed-width face for anything read as a listing rather than as prose.</summary>
        /// <remarks>
        /// Fixed width, so a column of line numbers and a column of indentation line up. Cut to the
        /// same line height and baseline as the game's own faces, so a line of it stands exactly as
        /// tall as a line anywhere else and any of the sizes above apply to it unchanged.
        /// <para>
        /// A subtype out of the definition registry, the same way <c>Symbols.Font</c> is, declared by
        /// the Maltek Mono mod this one requires.
        /// </para>
        /// </remarks>
        public const string Mono = "MaltekMono";
    }
}