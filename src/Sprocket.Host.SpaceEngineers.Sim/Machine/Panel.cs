using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using VRage.Game.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A button panel on the rig.
/// </summary>
/// <remarks>
/// <b>A button raises nothing unless its slot holds something.</b> That is the game's own rule and
/// the reason Sprocket writes its action into a slot at all, so the rig keeps it: pressing an empty
/// button is a press nobody hears.
/// </remarks>
sealed class Panel : Fitting
{
    Action<int> _pressed;

    public Panel(long id, string name, MyObjectBuilderType type, string subtype, string display,
        int buttons)
        : base(id, name, type, subtype, display)
    {
        Toolbar = new Toolbar(buttons);

        Fit(Pretend.To<IMyButtonPanel>()
            .Answers("add_ButtonPressed", invocation =>
            {
                _pressed = (Action<int>)Delegate.Combine(_pressed, (Delegate)invocation.Arguments[0]);
                return null;
            })
            .Answers("remove_ButtonPressed", invocation =>
            {
                _pressed = (Action<int>)Delegate.Remove(_pressed, (Delegate)invocation.Arguments[0]);
                return null;
            })
            .Does("IsButtonAssigned", arguments => Toolbar.Holds((int)arguments[0]))
            .Gives("AnyoneCanUse", () => true));
    }

    /// <summary>How many buttons this panel has.</summary>
    public int Buttons => Toolbar.SlotCount;

    /// <summary>
    /// What the game does when a slot is invoked: run whatever it holds, with what it holds it with.
    /// </summary>
    /// <remarks>
    /// Installed by whoever is running the rig, because what a slot's payload means is Sprocket's
    /// business and not a button's.
    /// <para>
    /// Handed the block the slot names as well as the payload, because the game runs an action
    /// <em>on a block</em>: a slot naming one that is gone does nothing at all. Without that, a
    /// press here reached Sprocket however broken the wiring was, and the rig answered a button the
    /// game would have left silent.
    /// </para>
    /// </remarks>
    public Action<long, string> Invokes;

    /// <summary>
    /// What the game does when a slot holds a block's own action: perform it on that block.
    /// </summary>
    /// <remarks>
    /// Installed by whoever is running the rig, because finding a block by its id is the construct's
    /// business. A trigger that does one plain thing is wired this way, and Sprocket is not part of
    /// the press at all - which is the property worth being able to test.
    /// </remarks>
    public Action<long, string> Performs;

    /// <summary>
    /// Somebody pressing one, counted from zero.
    /// </summary>
    /// <remarks>
    /// <b>An empty slot is a press nobody hears</b>, which is the game's own rule and the reason
    /// Sprocket writes its action into a slot at all.
    /// <para>
    /// What runs is what the slot holds. The panel raises its own event as well, because the game
    /// does, but nothing in Sprocket listens for one any more: a press arrives by the slot invoking
    /// an action, and sampling for it as well would start the same sequence twice.
    /// </para>
    /// </remarks>
    public void Press(int button)
    {
        if (!Enabled || !Toolbar.Holds(button))
            return;

        if (_pressed != null)
            _pressed(button);

        MyToolbarItemInfo held;
        if (!Toolbar.TryGetItem(button, out held))
            return;

        // A block's own action, which the game performs itself. Nothing of Sprocket's is involved and
        // the slot carries no argument.
        if (held.ActionId != TriggerAction.ActionId)
        {
            if (Performs != null)
                Performs(held.BlockEntityId, held.ActionId);

            return;
        }

        if (Invokes == null)
            return;

        _held.Clear();
        Toolbar.Parameters(button, _held);

        if (_held.Count > 0)
        {
            var key = _held[0].Value as string;
            if (!string.IsNullOrEmpty(key))
                Invokes(held.BlockEntityId, key);
        }
    }

    readonly List<TerminalActionParameter> _held = new List<TerminalActionParameter>();
}
