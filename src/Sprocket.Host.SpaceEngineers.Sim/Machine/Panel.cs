using System;
using System.Collections.Generic;
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
    readonly Dictionary<int, string> _labels = new Dictionary<int, string>();

    /// <summary>A panel of <paramref name="buttons" /> buttons, each a slot on its toolbar's one page.</summary>
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
            .Does("GetButtonName", arguments => Label((int)arguments[0]))
            .Does("HasCustomButtonName", arguments => _labels.ContainsKey((int)arguments[0]))
            .Does("SetCustomButtonName", arguments =>
            {
                _labels[(int)arguments[0]] = (string)arguments[1];
                Told("label " + arguments[0] + " " + arguments[1]);
            })
            .Does("ClearCustomButtonName", arguments =>
            {
                _labels.Remove((int)arguments[0]);
                Told("unlabel " + arguments[0]);
            })
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

    /// <summary>What a person reads on a button, counted from zero: the label a step wrote, or the game's own where none was.</summary>
    /// <remarks>The game's own reads "No action" for an empty slot; the rig says the same for every slot.</remarks>
    public string Label(int button)
    {
        return _labels.TryGetValue(button, out var label) ? label : "No action";
    }
}
