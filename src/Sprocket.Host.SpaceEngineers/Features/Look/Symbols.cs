using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>The shapes a screen is built from that letters cannot draw.</summary>
    public static class Symbols
    {
        /// <summary>The font every symbol here is drawn in.</summary>
        /// <remarks>
        ///     Declared by the Maltek Symbols mod this one requires; the glyphs are drawn by
        ///     <c>art/symbols/build_font.py</c>.
        /// </remarks>
        public const string Font = "MaltekSymbols";

        /// <summary>An arrow that turns back on itself, for walking a change back.</summary>
        public const string Undo = "u";

        /// <summary>The same arrow the other way, for walking it forward again.</summary>
        public const string Redo = "r";

        /// <summary>A bin, for destroying something.</summary>
        public const string Delete = "d";

        /// <summary>An open arrowhead, leftward.</summary>
        public const string Left = "<";

        /// <summary>The same arrowhead, upward.</summary>
        public const string Up = "^";

        /// <summary>The same arrowhead, rightward.</summary>
        public const string Right = ">";

        /// <summary>The same arrowhead, downward.</summary>
        public const string Down = "v";

        /// <summary>An unfilled circle: a choice not taken, where only one may be.</summary>
        public const string Ring = "o";

        /// <summary>The same circle filled: the one that was taken.</summary>
        public const string Disc = "0";

        /// <summary>Taking one away.</summary>
        public const string Minus = "-";

        /// <summary>Adding one.</summary>
        public const string Plus = "+";

        /// <summary>A shaft with a head on it: what one thing led to.</summary>
        public const string Arrow = "a";

        /// <summary>A tick: yes, in a box where several may be said yes to.</summary>
        public const string Check = "c";

        /// <summary>A triangle pointing the way a thing runs, for doing by hand what something else would do.</summary>
        public const string Play = "g";

        /// <summary>A square, for stopping by hand what is running.</summary>
        public const string Stop = "t";

        /// <summary>An arrow meeting a bar: something that runs until it is finished.</summary>
        public const string Finish = "w";

        /// <summary>A pencil, for changing what something is called.</summary>
        public const string Pencil = "p";

        /// <summary>A cross, for putting something away without destroying it.</summary>
        public const string Close = "x";

        /// <summary>Something below here is wrong, without saying what.</summary>
        public const string Warning = "!";

        /// <summary>A square loop with a break in it: read the ship again.</summary>
        public const string Refresh = "s";

        /// <summary>Two sheets, one behind the other: another of something that already exists.</summary>
        public const string Duplicate = "2";

        /// <summary>Two bars: held where it is and passed over, until it is let go again.</summary>
        public const string Pause = "e";

        /// <summary>A sheet ruled into four: a blueprint, the drawing logic systems are made from.</summary>
        public const string Blueprint = "b";

        /// <summary>Three steps beside an arrow down: a sequence, steps taken in order.</summary>
        public const string Sequence = "q";

        /// <summary>A square pulse: a signal, sent from a block's toolbar, and the slot it comes in through.</summary>
        public const string Signal = "n";

        /// <summary>A lightning bolt: something a block is told to do.</summary>
        public const string Action = "i";

        /// <summary>A clock: something a block is told to do that takes time, which a step waits for.</summary>
        public const string Awaited = "k";

        /// <summary>"=?": something a block is asked about, what one of its values is.</summary>
        public const string Value = "l";

        /// <summary>A cube: a block, where none in particular is meant, and the slot a block fills.</summary>
        public const string Block = "B";

        /// <summary>Every hue in order, red round to red.</summary>
        /// <remarks>
        ///     The game's own <c>Textures\GUI\HueScale.png</c>, which has no id a mod can use, so a material is
        ///     declared over it in <c>Content/Data/TransparentMaterials.sbc</c>.
        /// </remarks>
        public static readonly MyStringId Hue = MyStringId.GetOrCompute("Sprocket_Hue");
    }
}