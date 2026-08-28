using VRage.Utils;

namespace Phosphor
{
    /// <summary>
    /// The shapes a screen is built from that letters cannot draw.
    /// </summary>
    /// <remarks>
    /// <b>A font, not a set of sprites.</b> Both targets already draw text, and both resolve a font by
    /// subtype out of the one definition registry every loaded mod contributes to - so a glyph costs
    /// nothing at this seam, where a sprite costs a material declared once per target per icon, in two
    /// files that have to be kept agreeing. Measuring comes free with it, which is what lets a symbol sit
    /// inline in a sentence rather than only in a slot sized for it.
    /// <para>
    /// The font is drawn rather than borrowed. Space Engineers has no undo, no redo and no delete glyph -
    /// its only cross is <c>button_close_symbol</c>, which means dismiss, and using it for destroy was
    /// the mistake this replaces. What the game does have is used where it fits and nothing here
    /// duplicates it.
    /// </para>
    /// <para>
    /// The characters are readable on purpose. Nothing but symbols is ever drawn in this face, so there
    /// is no collision to design around, and a draw list that says <c>"d"</c> is one somebody can read.
    /// </para>
    /// </remarks>
    public static class Symbols
    {
        /// <summary>
        /// The font every symbol here is drawn in.
        /// </summary>
        /// <remarks>
        /// Shipped as content, so it has to be declared in a <c>Fonts.sbc</c> that the game loads. It is
        /// meant to become a mod of its own that display mods depend on rather than each carrying a copy -
        /// which is why nothing about it is named for Reactor.
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

        public const string Up = "^";

        public const string Right = ">";

        public const string Down = "v";

        /// <summary>An unfilled circle. A choice not taken, where only one may be.</summary>
        public const string Ring = "o";

        /// <summary>The same circle filled. The one that was taken.</summary>
        public const string Disc = "0";

        /// <summary>Taking one away. The inverse of the plus that added it.</summary>
        public const string Minus = "-";

        /// <summary>Adding one.</summary>
        public const string Plus = "+";

        /// <summary>
        /// A shaft with a head on it. What one thing led to.
        /// </summary>
        /// <remarks>
        /// Not the chevron. A chevron is a direction to travel and sits on something you press; this is a
        /// consequence and sits in a sentence, between what happened and what it caused. The game's font
        /// has no arrow at all, which is what this face exists for.
        /// </remarks>
        public const string Arrow = "a";

        /// <summary>
        /// A tick. Yes, in a box where several may be said yes to.
        /// </summary>
        /// <remarks>
        /// The game draws a ticked box as a smaller filled box inside a bigger one, and its indeterminate
        /// state as a bar. Faithful to itself, and it reads as "partly" to anybody whose experience of a
        /// checkbox came from anywhere else - which is this audience exactly. So this is one of the few
        /// places the game has something and we deliberately do not use it.
        /// </remarks>
        public const string Check = "c";

        /// <summary>
        /// Flat white, tinted to whatever colour is wanted. The only way to fill a rectangle, since the
        /// seam draws sprites and knows nothing of solid colours.
        /// </summary>
        /// <remarks>
        /// The one thing here that is still a material, because it is not a symbol - it is the absence of
        /// one, and a rectangle of colour is what every fill, rule and border on either target is made of.
        /// </remarks>
        public static readonly MyStringId Fill = MyStringId.GetOrCompute("Phosphor_Fill");
    }
}
