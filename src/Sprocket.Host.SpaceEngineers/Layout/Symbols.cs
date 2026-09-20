using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Layout
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
        /// about it is named for Sprocket.
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

        /// <summary>
        /// A triangle pointing the way a thing runs. Doing now, by hand, what something else would
        /// do.
        /// </summary>
        /// <remarks>
        /// The one symbol in this set that everybody already knows, which is why it is worth a glyph
        /// rather than two words: it is on every machine that has ever played anything. What it does
        /// still has to be said in the note on it, because "run" and "play" are not the same promise
        /// - a sequence started this way is started as its trigger would start it, not previewed.
        /// </remarks>
        public const string Play = "g";

        /// <summary>
        /// A square. Stopping by hand what is running.
        /// </summary>
        /// <remarks>
        /// The other half of <see cref="Play" />, and as widely known as it is: the two have shared
        /// a pair of buttons on every machine that plays anything for fifty years. It takes the
        /// place of the triangle rather than sitting beside it, because a sequence is either
        /// running or it is not and offering both would be offering one that does nothing.
        /// </remarks>
        public const string Stop = "t";

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
        /// It sits inside the part of a trigger that says what it does, where it empties that part
        /// rather than taking the trigger away: the bin at the end of that row is the other one.
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

        /// <summary>A square loop with a break in it: read the ship again.</summary>
        public const string Refresh = "s";

        /// <summary>Two sheets, one behind the other. Another of something that already exists.</summary>
        public const string Duplicate = "2";

        /// <summary>
        /// Flat white, tinted to whatever colour is wanted. The only way to fill a rectangle, since the
        /// seam draws sprites and knows nothing of solid colours.
        /// </summary>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Sprocket_Fill");

        /// <summary>
        /// Every hue in order, red round to red, for the one slider whose track says what it does.
        /// </summary>
        /// <remarks>
        /// The game's own <c>Textures\GUI\HueScale.png</c>, which its colour picker and its wardrobe both
        /// draw. Reached the same way as <see cref="Fill" />, by declaring a material over it, because the
        /// texture has no id of its own that a mod can use.
        /// </remarks>
        public static readonly MyStringId Hue = MyStringId.GetOrCompute("Sprocket_Hue");
    }
}
