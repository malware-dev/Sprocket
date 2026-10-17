using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What only the game can say about a block: what it can be told to do, and what its slots hold.
    /// </summary>
    public interface ISlots
    {
        /// <summary>
        /// The toolbar a block's own actions live on, or null where it has none.
        /// </summary>
        /// <remarks>The game's toolbar 0, which for a button panel is the buttons.</remarks>
        IMyToolbar Of(IMyTerminalBlock block);

        /// <summary>A block's toolbar by its place in the game's own order, from 0, or null where it has no such toolbar.</summary>
        IMyToolbar Of(IMyTerminalBlock block, int index);

        /// <summary>What a slot's item was given to run with, appended to <paramref name="into" />.</summary>
        void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into);

        /// <summary>Puts an item in a slot, and answers whether the write took.</summary>
        bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters);

        /// <summary>Empties a slot, and answers whether that took.</summary>
        bool Clear(IMyToolbar toolbar, int slot);

        /// <summary>
        /// What a slot's item does, in the game's words: the block or group and its action, or the thing it holds; with the
        /// picture the game draws for it in <paramref name="picture" />, empty where there is none.
        /// </summary>
        string Described(MyToolbarItemInfo item, out string picture);

        /// <summary>Whether a block has an action of the game's own by that name.</summary>
        bool Offers(IMyTerminalBlock block, string actionId);

        /// <summary>
        /// Everything this block declares it can be told to do, appended to <paramref name="into" />.
        /// </summary>
        /// <remarks>Asked once per block type, not per block.</remarks>
        void Offered(IMyTerminalBlock block, List<Offer> into);

        /// <summary>Every value this block carries that can be told what to be, appended to <paramref name="into" />.</summary>
        void Settable(IMyTerminalBlock block, List<Offer> into);

        /// <summary>Sets the block's value <paramref name="propertyId" /> to <paramref name="value" />, answering whether it was set; a value of the wrong kind is refused.</summary>
        bool Set(IMyTerminalBlock block, string propertyId, Literal value);

        /// <summary>One of the block's own values made ready to be read, or null where it has no such value.</summary>
        /// <remarks>Asked once, when a program is bound.</remarks>
        Gauge Gauge(IMyTerminalBlock block, string propertyId);

        /// <summary>
        /// One of the block's own actions or values made ready to be given, or null where it has no
        /// such thing or the value is not the kind it holds.
        /// </summary>
        /// <remarks>Asked once, when a program is bound.</remarks>
        BlockOrder Prepare(IMyTerminalBlock block, string id, ReachKind form, IList<Literal> arguments,
            IList<Primitive> types);

        /// <summary>
        /// Performs one of the block's own actions, and answers whether there was one to perform.
        /// </summary>
        /// <remarks>An id the block does not have answers false, and never throws.</remarks>
        bool Perform(IMyTerminalBlock block, string actionId, IList<Literal> arguments);

        /// <summary>
        /// Whether the block a slot names is still anywhere in the world.
        /// </summary>
        /// <remarks>Looks through the whole world: a slot may name a block on a grid that has since undocked.</remarks>
        bool Exists(long entityId);
    }
}
