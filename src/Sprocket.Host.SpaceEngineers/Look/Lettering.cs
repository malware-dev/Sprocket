
namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>
    /// Text sizes, named for what they are for.
    /// </summary>
    /// <remarks>
    /// Font scales rather than pixels, because this draws into a fixed UI size that the game stretches
    /// to the monitor. Named rather than written out so that a size means the same thing everywhere it
    /// is used.
    /// </remarks>
    public static class Lettering
    {
        /// <summary>
        /// A screen's name. The one size the game has no equivalent for, because it has no screens.
        /// </summary>
        public const float Title = 0.9f;

        /// <summary>
        /// Everything that is a sentence, a row or a label. <b>The size the game writes in.</b>
        /// </summary>
        /// <remarks>
        /// Measured rather than chosen. The terminal is almost perfectly uniform - list rows, section
        /// headings, field contents, button captions and the window's own title are all one size, a cap
        /// height of fourteen units - and this is the scale that lands there. The first cut of these was
        /// a quarter smaller across the board, which is what a panel drawn against a mock-up looks like
        /// rather than one drawn against the game beside it.
        /// </remarks>
        public const float Body = 0.64f;

        /// <summary>
        /// A step down, for a control rather than a sentence.
        /// </summary>
        /// <remarks>
        /// Also the game's, and the only other size it has: its tabs and the buttons along the top of
        /// the terminal are cut back to about twelve and a half units, and nothing in it goes below that.
        /// </remarks>
        public const float Small = 0.57f;

        /// <summary>
        /// The quietest thing that still carries a word - a count, a "ran 4s ago", a kind under a name.
        /// </summary>
        /// <remarks>
        /// Below anything the game draws, which is why it is one step rather than three. Sprocket has a
        /// tier of detail the terminal has no equivalent for, and it has to be smaller than the name it
        /// belongs to; it does not have to be half of it.
        /// </remarks>
        public const float Tiny = 0.5f;

        /// <summary>
        /// A symbol standing beside a line of text.
        /// </summary>
        /// <remarks>
        /// Larger than the text it accompanies, and deliberately. A letter has its neighbours to lean on
        /// and a symbol has none - it is read alone and at a glance, and the first cut of these was drawn
        /// at the same size as small text and came out as a smudge.
        /// </remarks>
        public const float Symbol = 0.57f;

        /// <summary>
        /// How far to lift a symbol that stands on a line of the game's text, as a fraction of it.
        /// </summary>
        /// <remarks>
        /// <b>This describes the game's face, not the symbols.</b> Space Engineers sets its letters
        /// slightly high in their own line, so a mark centred in its line - which is what a symbol font
        /// owes anyone who draws it - reads as sitting low beside them. The correction belongs here
        /// rather than in the font, because baking it into the artwork would fit one consumer's quirk
        /// into something meant to be used by anybody. It was baked in three times before this.
        /// <para>
        /// <b>Measured in the game, on the one case it is for.</b> A symbol beside the heading sat nine
        /// pixels below the middle of its capitals on a 3840-wide screen, which is four and a half
        /// units, which is this much of a symbol. Nothing else referees it: the simulator's rule for
        /// placing a glyph against a line is its own reconstruction and disagrees with the game, and a
        /// recorded frame cannot see it at all - a frame records the call, and this moves the ink
        /// inside the page the call names.
        /// </para>
        /// <para>
        /// It does not apply to a symbol centred in a row, which is not standing on a line of anything
        /// - see <c>Widgets.Centred</c>. Those land on the row's own middle with no help, measured to
        /// within two pixels, and lifting them is what made them look top-aligned.
        /// </para>
        /// </remarks>
        public const float SymbolLift = 0.123f;

        /// <summary>
        /// The face for anything read as a listing rather than as prose.
        /// </summary>
        /// <remarks>
        /// Fixed width, so a column of line numbers and a column of indentation line up. Cut to the
        /// same line height and baseline as the game's own faces, so a line of it stands exactly as
        /// tall as a line anywhere else and any of the sizes above apply to it unchanged.
        /// <para>
        /// A subtype out of the definition registry, the same way <c>Symbols.Font</c> is, and it comes
        /// from the same place - see <c>Content/Data/Fonts.sbc</c>.
        /// </para>
        /// </remarks>
        public const string Mono = "MaltekMono";
    }
}
