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

        /// <summary>An arrow that turns forward on itself, for walking a change forward again.</summary>
        public const string Redo = "r";

        /// <summary>A bin, for destroying something.</summary>
        public const string Delete = "d";

        /// <summary>An open arrowhead, leftward.</summary>
        public const string Left = "<";

        /// <summary>An open arrowhead, upward.</summary>
        public const string Up = "^";

        /// <summary>An open arrowhead, rightward.</summary>
        public const string Right = ">";

        /// <summary>An open arrowhead, downward.</summary>
        public const string Down = "v";

        /// <summary>An unfilled circle: a choice not taken, where only one may be.</summary>
        public const string Ring = "o";

        /// <summary>A filled circle: the choice taken, where only one may be.</summary>
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

        /// <summary>A logic system's chip cut out of a square: a blueprint, the mold logic systems are made from.</summary>
        public const string Blueprint = "b";

        /// <summary>A chip with its pins along two sides: a logic system, the working thing a blueprint is the mold of.</summary>
        public const string LogicSystem = "y";

        /// <summary>The logic system's chip in front of a darker one: an installation, one of however many of a logic system.</summary>
        public const string Installation = "I";

        /// <summary>Three steps beside an arrow down: a sequence, steps taken in order.</summary>
        public const string Sequence = "q";

        /// <summary>A square pulse: a signal a block sends, and the slot it comes in through.</summary>
        public const string Signal = "n";

        /// <summary>A lightning bolt: a trigger, what starts a sequence.</summary>
        public const string Trigger = "T";

        /// <summary>An arrow into a block: something a block is told to do.</summary>
        public const string Action = "i";

        /// <summary>An arrow into a clock: something a block is told to do that takes time, which a step can wait for.</summary>
        public const string Lasting = "A";

        /// <summary>A clock: a wait for what is running.</summary>
        public const string Awaited = "k";

        /// <summary>The outline round <see cref="Lasting" />, lit round it while it is tied to what is under the pointer.</summary>
        public const string LastingOutline = "W";

        /// <summary>The outline round <see cref="Awaited" />, lit round it while it is tied to what is under the pointer.</summary>
        public const string AwaitedOutline = "K";

        /// <summary>"=?": something a block is asked about, what one of its values is.</summary>
        public const string Value = "l";

        /// <summary>A cube: a block, where none in particular is meant, and the slot a block fills.</summary>
        public const string Block = "B";

        /// <summary>An open eye over a toolbar: blocks hidden from toolbars are shown.</summary>
        public const string HiddenShown = "h";

        /// <summary>A shut eye over a toolbar: blocks hidden from toolbars stay hidden.</summary>
        /// <remarks>Drawn on the open eye's frame, so one replaces the other without the toolbar moving.</remarks>
        public const string HiddenHidden = "H";

        /// <summary>The outline lit round <paramref name="mark" />, or null for a mark that is never lit.</summary>
        public static string OutlineOf(string mark)
        {
            return mark == Lasting ? LastingOutline : mark == Awaited ? AwaitedOutline : null;
        }
    }
}