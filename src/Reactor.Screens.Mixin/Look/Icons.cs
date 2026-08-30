using System.Collections.Generic;
using VRage.Utils;
using Reactor.Ui;
using VRageMath;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Look
{
    /// <summary>
    /// Turns core's name for a kind of thing into the material that draws it.
    /// </summary>
    /// <remarks>
    /// One function, on purpose. Today it makes a name up and the backend behind it keeps a table of
    /// where Space Engineers happens to store that artwork - which is a stand-in for what the mod will do
    /// instead, which is ask the block for its own icon. When that arrives this is the only thing that
    /// changes.
    /// </remarks>
    public static class Icons
    {
        /// <summary>What every block icon's material name begins with.</summary>
        /// <remarks>
        /// So that a backend standing in for the game can tell which of its materials are block pictures
        /// and go and find them. The game itself needs no such thing - it has the block.
        /// </remarks>
        public const string Prefix = "Reactor_Block_";

        public static MyStringId MaterialFor(string name)
        {
            // A slash means it is the game's own block definition id, and the screen-space API resolves
            // one to that block's picture by itself. Anything without a slash is a word made up by a host
            // that has no blocks to ask, and gets the prefix a stand-in backend goes looking for.
            if (name != null && name.IndexOf('/') >= 0)
                return MyStringId.GetOrCompute(name);

            return MyStringId.GetOrCompute(Prefix + name);
        }
    }
}
