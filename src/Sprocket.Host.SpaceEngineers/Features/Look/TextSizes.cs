
namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>Text sizes, as the height of a line in UI units, named for what they are for.</summary>
    /// <remarks>
    ///     Exact conversions of scales measured in the game: a line of the game's text is 37 units at a scale of
    ///     one, and a line of the symbol font 64.
    /// </remarks>
    public static class TextSizes
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

        /// <summary>How far below the top of a line of the game's text its capitals start, as a fraction of the line.</summary>
        /// <remarks>Read from the game's font page, <c>Fonts/white/FontDataPA</c>: an H's ink starts eight units into a line 37 tall.</remarks>
        public const float CapTop = 0.22f;

        /// <summary>How far down a line of the game's text its baseline sits, as a fraction of the line.</summary>
        /// <remarks>The game's font file, <c>Fonts/white/FontDataPA.xml</c>: a base of 30 in a line 37 tall.</remarks>
        public const float Baseline = 0.81f;

        /// <summary>A symbol standing beside a line of text.</summary>
        public const float Symbol = 36.48f;

        /// <summary>The pencil beside a name that can be changed, a step above an ordinary symbol.</summary>
        public const float Pen = 40.96f;

        /// <summary>How far to lift a symbol that stands on a line of the game's text, as a fraction of it.</summary>
        /// <remarks>
        /// Measured in the game: a symbol beside the heading sat nine pixels below the middle of its capitals on a 3840-wide
        /// screen, four and a half units.
        /// </remarks>
        public const float SymbolLift = 0.123f;

        /// <summary>The fixed-width face for anything read as a listing rather than as prose.</summary>
        /// <remarks>
        /// Cut to the same line height and baseline as the game's own faces; a subtype declared by the Maltek Mono mod this one
        /// requires.
        /// </remarks>
        public const string Mono = "MaltekMono";

        /// <summary>The Maltek proportional face, for reading prose.</summary>
        /// <remarks>
        /// Cut to the same line height and baseline as the game's own faces and <see cref="Mono" />, from Inter. A subtype
        /// declared by the Maltek Sans mod.
        /// </remarks>
        public const string Sans = "MaltekSans";

        /// <summary>The bold weight of <see cref="Sans" />, for strong words.</summary>
        public const string SansBold = "MaltekSansBold";

        /// <summary>The italic of <see cref="Sans" />, for emphasized words.</summary>
        public const string SansItalic = "MaltekSansItalic";

        /// <summary>The bold italic of <see cref="Sans" />, for words both strong and emphasized.</summary>
        public const string SansBoldItalic = "MaltekSansBoldItalic";

        /// <summary>The game's own face in bold, for strong words.</summary>
        /// <remarks>Cut in exactly the layout of the game's face; a subtype declared by the Space Engineers Font Styles mod.</remarks>
        public const string Bold = "SpaceEngineersBold";

        /// <summary>The game's own face in italic, for emphasized words.</summary>
        public const string Italic = "SpaceEngineersItalic";

        /// <summary>The game's own face in bold italic, for words both strong and emphasized.</summary>
        public const string BoldItalic = "SpaceEngineersBoldItalic";
    }
}