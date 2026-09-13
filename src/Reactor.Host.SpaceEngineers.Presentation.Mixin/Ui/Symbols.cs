using VRage.Utils;

namespace Reactor.Host.SpaceEngineers.Presentation.Ui
{
    /// <summary>The shapes a screen is built from that letters cannot draw.</summary>
    /// <remarks>
    /// Space Engineers has no undo, redo, delete or arrow glyph of its own. Its only cross is
    /// <c>button_close_symbol</c>, which means dismiss.
    /// <para>
    /// Nothing but symbols is ever drawn in this face, so the characters can be readable letters
    /// without colliding with anything: a draw list that says <c>"d"</c> is one somebody can read.
    /// </para>
    /// </remarks>
    public static class Symbols
    {
        /// <summary>The font every symbol here is drawn in.</summary>
        /// <remarks>
        /// Shipped as content, so it has to be declared in a <c>Fonts.sbc</c> the game loads. Nothing
        /// about it is named for Reactor.
        /// </remarks>
        public const string Font = "MaltekSymbols";

        /// <summary>An arrow that turns back on itself. Walking a change back.</summary>
        public const string Undo = "u";

        /// <summary>The same arrow the other way. Walking it forward again.</summary>
        public const string Redo = "r";

        /// <summary>A bin. Destroying something, as opposed to putting it away.</summary>
        public const string Delete = "d";

        /// <summary>An open arrowhead. Where pressing this goes.</summary>
        public const string Left = "<";

        /// <summary>The same arrowhead, upward.</summary>
        public const string Up = "^";

        /// <summary>The same arrowhead, rightward.</summary>
        public const string Right = ">";

        /// <summary>The same arrowhead, downward.</summary>
        public const string Down = "v";

        /// <summary>An unfilled circle. A choice not taken, where only one may be.</summary>
        public const string Ring = "o";

        /// <summary>The same circle filled. The one that was taken.</summary>
        public const string Disc = "0";

        /// <summary>Taking one away. The inverse of the plus that added it.</summary>
        public const string Minus = "-";

        /// <summary>Adding one.</summary>
        public const string Plus = "+";

        /// <summary>A shaft with a head on it. What one thing led to.</summary>
        /// <remarks>
        /// Not <see cref="Right" />: a chevron is a direction to travel and sits on something you
        /// press, where this is a consequence and sits in a sentence.
        /// </remarks>
        public const string Arrow = "a";

        /// <summary>A tick. Yes, in a box where several may be said yes to.</summary>
        /// <remarks>
        /// Deliberately not the game's, which draws a ticked box as a smaller filled box inside a
        /// bigger one and its indeterminate state as a bar. That reads as "partly" to anybody whose
        /// experience of a checkbox came from anywhere else.
        /// </remarks>
        public const string Check = "c";

        /// <summary>A pencil. Changing what something is called, as opposed to what it does.</summary>
        public const string Pencil = "p";

        /// <summary>
        /// A cross. Putting something away, never destroying what is on it - that is
        /// <see cref="Delete" />, and the two are deliberately nothing alike.
        /// </summary>
        /// <remarks>
        /// The game's own cross cannot be borrowed: it belongs to a control on the host screen, and the
        /// harness draws its controls before handing over to the mod, so the first opaque thing a mod
        /// paints buries it. Anything drawing its own panel has to carry its own way off it.
        /// <para>
        /// Nothing draws this yet. The chevron in the header leaves every screen, so there is no use
        /// for a second way out until there are dialogs.
        /// </para>
        /// </remarks>
        public const string Close = "x";

        /// <summary>
        /// Something below here is wrong, without saying what.
        /// </summary>
        /// <remarks>
        /// Plain rather than inside a triangle: three shapes competing for twenty pixels loses the
        /// outline first. See <c>art/symbols/build_font.py</c>, which draws it.
        /// </remarks>
        public const string Warning = "!";

        /// <summary>
        /// Flat white, tinted to whatever colour is wanted. The only way to fill a rectangle, since the
        /// seam draws sprites and knows nothing of solid colours.
        /// </summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Reactor_Fill");
    }
}
