namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>Text sizes as font scales, named for what they are for.</summary>
    public static class Lettering
    {
        /// <summary>A screen's name.</summary>
        public const float Title = 0.9f;

        /// <summary>Everything that is a sentence, a row or a label.</summary>
        /// <remarks>
        ///     Measured in the game: the terminal's list rows, section headings, field contents, button
        ///     captions and window title all have a cap height of fourteen units, which this scale lands on.
        /// </remarks>
        public const float Body = 0.64f;

        /// <summary>A step down, for a control rather than a sentence.</summary>
        /// <remarks>
        ///     Measured in the game: the terminal's tabs and top buttons have a cap height of about twelve and
        ///     a half units, the smallest text it draws.
        /// </remarks>
        public const float Small = 0.57f;

        /// <summary>The quietest thing that still carries a word: a count, a "ran 4s ago", a kind under a name.</summary>
        public const float Tiny = 0.5f;

        /// <summary>A symbol standing beside a line of text.</summary>
        public const float Symbol = 0.57f;

        /// <summary>How far to lift a symbol that stands on a line of the game's text, as a fraction of it.</summary>
        /// <remarks>
        ///     The game sets its letters slightly high in their line, so a symbol centred in its own sits low
        ///     beside them. Measured in the game: a symbol beside the heading sat nine pixels below the middle
        ///     of its capitals on a 3840-wide screen, four and a half units. Only the game can check this; the
        ///     simulator places glyphs by its own rule, and a recorded frame cannot see it.
        ///     <para>
        ///         Not for a symbol centred in a row (<c>Widgets.Centred</c>), which lands on the row's middle
        ///         unaided, measured to within two pixels.
        ///     </para>
        /// </remarks>
        public const float SymbolLift = 0.123f;

        /// <summary>The fixed-width face for anything read as a listing rather than as prose.</summary>
        /// <remarks>
        ///     Cut to the same line height and baseline as the game's own faces, so the sizes above apply to it
        ///     unchanged. Declared in <c>Content/Data/Fonts.sbc</c>.
        /// </remarks>
        public const string Mono = "MaltekMono";
    }
}