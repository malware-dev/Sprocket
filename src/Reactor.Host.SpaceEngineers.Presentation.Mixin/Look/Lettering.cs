using VRage.Game;
using VRage.Utils;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Host.SpaceEngineers.Presentation.Look
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
        /// Below anything the game draws, which is why it is one step rather than three. Reactor has a
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
    }
}
