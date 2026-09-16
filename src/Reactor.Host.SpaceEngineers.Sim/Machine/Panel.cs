using System;
using Reactor.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Reactor.Sim.Machine;

/// <summary>
/// A button panel on the rig.
/// </summary>
/// <remarks>
/// <b>A button raises nothing unless its slot holds something.</b> That is the game's own rule and
/// the reason Reactor writes its action into a slot at all, so the rig keeps it: pressing an empty
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

    /// <summary>Somebody pressing one, counted from zero.</summary>
    public void Press(int button)
    {
        if (Enabled && Toolbar.Holds(button) && _pressed != null)
            _pressed(button);
    }
}
