using VRage.Game.ModAPI;
using Sandbox.ModAPI;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// How a block's toolbar is reached, said once.
    /// </summary>
    /// <remarks>
    /// <b>A block does not implement <see cref="IMyToolbarProvider" />, it carries one.</b> So
    /// <c>block as IMyToolbarProvider</c> compiles, returns null on every block in the game, and
    /// takes every toolbar read and write quietly with it - nothing throws, buttons simply never
    /// wire up. Reactor had that cast written out in three places and got it wrong in all three
    /// when the API moved the provider onto the component container.
    /// <para>
    /// Which is the reason this exists at all: one statement of how a toolbar is found, so the next
    /// change to that answer is one edit rather than a hunt.
    /// </para>
    /// </remarks>
    static class Toolbars
    {
        /// <summary>
        /// The toolbar a block's own actions live on, or null where it has none.
        /// </summary>
        /// <remarks>
        /// Index 0 is the primary one, which for a button panel is the buttons. A block with no
        /// toolbars does not carry the component at all, so failing to get it is the ordinary
        /// answer rather than a fault.
        /// </remarks>
        public static IMyToolbar Of(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            IMyToolbarProvider provider;
            if (!block.Components.TryGet(out provider) || provider.ToolbarCount == 0)
                return null;

            return provider.GetToolbar(0);
        }
    }
}
