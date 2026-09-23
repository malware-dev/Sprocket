using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What only the game can say about a block: what it can be told to do, and what its slots hold.
    /// </summary>
    /// <remarks>
    /// The only record of which button runs which sequence lives in the game's toolbars, so this is
    /// the surface that whole story rests on: find a block's toolbar, read what a slot holds, write a
    /// slot, clear one. A block's own actions are here for the same reason - they are the game's to
    /// declare, and a slot is written with one.
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

        /// <summary>Whether a block has an action of the game's own by that name.</summary>
        bool Offers(IMyTerminalBlock block, string actionId);

        /// <summary>
        /// Everything this block declares it can be told to do, appended to <paramref name="into" />.
        /// </summary>
        /// <remarks>
        /// The whole of what Sprocket knows about most blocks. Asked once per block type rather than
        /// per block - see <see cref="BlockKinds" /> - because a definition declares the same actions
        /// whichever one of it is asked.
        /// </remarks>
        void Offered(IMyTerminalBlock block, List<Offer> into);

        void Settable(IMyTerminalBlock block, List<Offer> into);

        bool Set(IMyTerminalBlock block, string propertyId, Literal value);

        /// <summary>
        /// One of the block's own actions or values made ready to be given, or null where it has no
        /// such thing or the value is not the kind it holds.
        /// </summary>
        /// <remarks>
        /// Asked once, when a program is bound; each value is built as the type in
        /// <paramref name="types" /> where there is one, so giving the order does nothing else.
        /// </remarks>
        Order Prepare(IMyTerminalBlock block, string id, Form form, IList<Literal> values,
            IList<Primitive> types);

        /// <summary>
        /// Performs one of the block's own actions, and answers whether there was one to perform.
        /// </summary>
        /// <remarks>
        /// The block is the authority on what it offers, so an id it does not have is answered with
        /// no rather than an error: a saved sequence can outlive the vocabulary it was written
        /// against, which is a thing to report calmly and not to throw over.
        /// <para>
        /// Turning each value into what the action wanted happens here, against the default it was
        /// declared with, because that is the only place the game's own types are known at all.
        /// </para>
        /// </remarks>
        bool Perform(IMyTerminalBlock block, string actionId, IList<Literal> values);

        /// <summary>
        /// Whether the block a slot names is still anywhere in the world.
        /// </summary>
        /// <remarks>
        /// A slot holds an id whether or not there is still a block behind it, and the difference
        /// decides whether Sprocket may write over it: a slot naming a block that is gone is wiring
        /// that nothing can honour, while one naming a live block is somebody's, even when that
        /// somebody is another Sprocket.
        /// <para>
        /// The whole world rather than this construct, because a slot may legitimately name a block
        /// on a grid that has since undocked - and taking that for gone would be Sprocket helping
        /// itself to somebody else's button.
        /// </para>
        /// </remarks>
        bool Exists(long entityId);
    }
}
