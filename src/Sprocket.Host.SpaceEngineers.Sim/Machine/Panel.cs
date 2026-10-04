using System;
using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A button panel on the rig.
/// </summary>
/// <remarks>
/// <b>A button raises nothing unless its slot holds something.</b> That is the game's own rule and
/// the reason Sprocket writes its action into a slot at all, so the rig keeps it: pressing an empty
/// button is a press nobody hears.
/// </remarks>
public sealed class Panel : Fitting
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

        Run(button);
    }
}
