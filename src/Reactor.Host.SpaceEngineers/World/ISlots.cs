using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// A construct's toolbar slots: where Reactor's own wiring is kept.
    /// </summary>
    /// <remarks>
    /// The only record of which button runs which sequence lives in the game's toolbars, so this is
    /// the surface that whole story rests on: find a block's toolbar, read what a slot holds, write a
    /// slot, clear one.
    /// <para>
    /// Said once, behind an interface, for two reasons. The game's conveniences for this reach past
    /// <c>IMyToolbar</c> into its own concrete item classes, so nothing but the game can answer them -
    /// the harness writes a slot and reads nothing back. And a toolbar is where the trigger work lands
    /// next, which is easier against one surface than against calls spread over three files.
    /// </para>
    /// </remarks>
    public interface ISlots
    {
        /// <summary>
        /// The toolbar a block's own actions live on, or null where it has none.
        /// </summary>
        /// <remarks>
        /// Index 0 is the primary one, which for a button panel is the buttons. A block with no
        /// toolbars does not carry the component at all, so failing to get one is the ordinary answer
        /// rather than a fault.
        /// </remarks>
        IMyToolbar Of(IMyTerminalBlock block);

        /// <summary>What a slot's item was given to run with, appended to <paramref name="into" />.</summary>
        void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into);

        /// <summary>Puts an item in a slot, and answers whether the write took.</summary>
        bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters);

        /// <summary>Empties a slot, and answers whether that took.</summary>
        bool Clear(IMyToolbar toolbar, int slot);
    }
}
