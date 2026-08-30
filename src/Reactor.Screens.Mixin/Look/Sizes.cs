using VRage.Game;
using VRage.Utils;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Look
{
    /// <summary>Things that are a size rather than a gap.</summary>
    public static class Sizes
    {
        /// <summary>A state light.</summary>
        public const float Dot = 13f;

        /// <summary>
        /// A block's picture.
        /// </summary>
        /// <remarks>
        /// Entirely this target's number. Core hands over a name and says nothing about size, which it
        /// should not: Space Engineers draws recognisable artwork for every block type and it earns the
        /// room, where a game whose things all look alike would want it smaller or absent.
        /// </remarks>
        public const float Icon = 54f;

        /// <summary>One stage of a pick.</summary>
        /// <remarks>
        /// Near enough the width the terminal gives its own block list, which was not the reason for the
        /// number and is a good check on it. There is room for it: a mod screen is handed the same box
        /// the terminal draws in, and two of these columns leave a third of it still empty.
        /// </remarks>
        public const float Column = 460f;

        /// <summary>Anything hanging over the screen - a question, a list that opens downward.</summary>
        public const float Popup = 512f;

        /// <summary>The box at the head of a row in a list of several things to choose between.</summary>
        public const float Mark = 17f;

        /// <summary>
        /// How narrow an editable name may get before it stops being something anybody can hit.
        /// </summary>
        /// <remarks>
        /// Reached by clearing a name, which is an ordinary act rather than a mistake - an unnamed
        /// sequence reads as its own sentence and is no worse off. What is not ordinary is a heading that
        /// shrinks to a point the moment the last character comes out of it.
        /// <para>
        /// Zero, meaning a plate falls back to its own height - a square, and no more. It held a hint once
        /// and was sized for one; now it holds a caret and a well, and every unit past a square is room
        /// kept for words that are not there, in the gap between a name and its pencil.
        /// </para>
        /// </remarks>
        public const float Name = 0f;

        /// <summary>
        /// A scrollbar, across. The game reserves twelve units for its own and so do we.
        /// </summary>
        public const float Bar = 12f;

        /// <summary>
        /// How short a thumb may get before it stops telling the truth about how much is out of sight.
        /// </summary>
        /// <remarks>
        /// Roughly two rows tall. Below that it is quicker to reach for the wheel than to catch it, which
        /// makes the bar decoration.
        /// </remarks>
        public const float Thumb = 34f;
    }
}
