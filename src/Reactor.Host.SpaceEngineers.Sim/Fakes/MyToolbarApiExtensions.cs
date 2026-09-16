using System.Collections.Generic;
using Reactor.Sim.Machine;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// The toolbar conveniences the mod calls, as the harness answers them.
    /// </summary>
    /// <remarks>
    /// The game's own are in Sandbox.Game, which the harness does not reference - see
    /// build/Reactor.GameReferences.props. Only what the mod uses is here, and the names and
    /// signatures are the game's so the mod's own sources compile against either.
    /// </remarks>
    public static class MyToolbarApiExtensions
    {
        public static void GetParameters(this IMyToolbarSlots slots, int index,
            List<TerminalActionParameter> output)
        {
            var toolbar = slots as Toolbar;
            if (toolbar != null)
                toolbar.Parameters(index, output);
        }

        public static IMyToolbarChangeBuilder Set(this IMyToolbarChangeBuilder builder, int index,
            MyToolbarItemInfo item, List<TerminalActionParameter> parameters)
        {
            var changes = builder as Toolbar.Changes;
            return changes == null ? builder : changes.Set(index, item, parameters);
        }
    }
}
