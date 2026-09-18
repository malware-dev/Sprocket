using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// The game's own toolbars.
    /// </summary>
    /// <remarks>
    /// <b>A block does not implement <see cref="IMyToolbarProvider" />, it carries one.</b> So
    /// <c>block as IMyToolbarProvider</c> compiles, returns null on every block in the game, and takes
    /// every toolbar read and write quietly with it - nothing throws, buttons simply never wire up.
    /// Reactor had that cast written out in three places and got it wrong in all three when the API
    /// moved the provider onto the component container.
    /// </remarks>
    public sealed class GameSlots : ISlots
    {
        public IMyToolbar Of(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            IMyToolbarProvider provider;
            if (!block.Components.TryGet(out provider) || provider.ToolbarCount == 0)
                return null;

            return provider.GetToolbar(0);
        }

        public void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into)
        {
            if (toolbar != null)
                toolbar.GetParameters(slot, into);
        }

        public bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters)
        {
            return toolbar != null && toolbar.BeginChanges().Set(slot, item, parameters).Apply();
        }

        public bool Exists(long entityId)
        {
            return MyAPIGateway.Entities != null && MyAPIGateway.Entities.EntityExists(entityId);
        }

        public bool Clear(IMyToolbar toolbar, int slot)
        {
            return toolbar != null && toolbar.BeginChanges().Clear(slot).Apply();
        }

        /// <summary>
        /// Whether a block has an action of the game's own by that name.
        /// </summary>
        /// <remarks>
        /// The block is asked rather than a list being kept, because which actions a block offers
        /// depends on its subtype and on what the game has done to it since - and a name that is
        /// wrong here would write a slot that looks wired and does nothing.
        /// </remarks>
        public bool Offers(IMyTerminalBlock block, string actionId)
        {
            return block != null && block.GetActionWithName(actionId) != null;
        }
    }
}
