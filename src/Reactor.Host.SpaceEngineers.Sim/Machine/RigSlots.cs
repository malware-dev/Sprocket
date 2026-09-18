using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Sim.Machine;

/// <summary>
/// The rig's toolbars, as the mod reads and writes them.
/// </summary>
/// <remarks>
/// The game's own conveniences for this cannot be answered by anything but the game: they reach past
/// <c>IMyToolbar</c> into its concrete item classes, so a fake toolbar is written to and then reads
/// back nothing at all - silently, which is the worst way for it to fail. This answers the same calls
/// over the rig's own slots instead.
/// </remarks>
sealed class RigSlots : ISlots
{
    readonly Rig _rig;

    public RigSlots(Rig rig)
    {
        _rig = rig;
    }

    public IMyToolbar Of(IMyTerminalBlock block)
    {
        if (block == null)
            return null;

        // The rig knows which of its own blocks this is, which is the same question the game answers
        // by looking for a toolbar component on it.
        foreach (Fitting fitting in _rig.Fitted)
        {
            if (fitting.Block == block)
                return fitting.Toolbar;
        }

        return null;
    }

    public void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into)
    {
        var rigs = toolbar as Toolbar;
        if (rigs != null)
            rigs.Parameters(slot, into);
    }

    public bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
        List<TerminalActionParameter> parameters)
    {
        var rigs = toolbar as Toolbar;
        if (rigs == null)
            return false;

        // The parameters go with the item, which the game's own change builder only accepts through a
        // convenience method it alone can answer. Here they are one call.
        Toolbar.Changes changes = rigs.Changing();
        changes.Set(slot, item, parameters);
        return changes.Apply();
    }

    public bool Offers(IMyTerminalBlock block, string actionId)
    {
        foreach (Fitting fitting in _rig.Fitted)
        {
            if (fitting.Block == block)
                return fitting.Offers(actionId);
        }

        return false;
    }

    public void Offered(IMyTerminalBlock block, List<Offer> into)
    {
        foreach (Fitting fitting in _rig.Fitted)
        {
            if (fitting.Block != block)
                continue;

            fitting.Offered(into);
            return;
        }
    }

    public bool Perform(IMyTerminalBlock block, string actionId, IList<Literal> values)
    {
        foreach (Fitting fitting in _rig.Fitted)
        {
            if (fitting.Block != block || !fitting.Offers(actionId))
                continue;

            fitting.Perform(actionId, values);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the rig still has the block a slot names.
    /// </summary>
    /// <remarks>
    /// The rig is the whole world here, so removing a fitting is what the game does when a block is
    /// ground off, cut, or left behind on a grid that undocked - which is the case this answers.
    /// </remarks>
    public bool Exists(long entityId)
    {
        foreach (Fitting fitting in _rig.Fitted)
        {
            if (fitting.Block.EntityId == entityId)
                return true;
        }

        return false;
    }

    public bool Clear(IMyToolbar toolbar, int slot)
    {
        var rigs = toolbar as Toolbar;
        if (rigs == null)
            return false;

        Toolbar.Changes changes = rigs.Changing();
        changes.Clear(slot);
        return changes.Apply();
    }
}
