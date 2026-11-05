using SpaceEngineers.Game.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A timer block on the rig: a toolbar whose every slot runs when the timer goes off.</summary>
/// <remarks>
/// Here because a trigger is any toolbar slot, not a button: a timer's slots are triggers exactly as a panel's are, and
/// nothing about the timer declares them. The countdown is left out; a test says when it goes off.
/// </remarks>
public sealed class TimerBlock : Fitting
{
    /// <summary>The slots a timer block's toolbar has.</summary>
    const int Slots = 9;

    /// <summary>A timer block known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public TimerBlock(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Toolbar = new Toolbar(Slots);
        Fit(Pretend.To<IMyTimerBlock>());
    }

    /// <summary>The timer going off: every slot of its toolbar runs, in order, where the timer is on.</summary>
    public void GoOff()
    {
        if (!Enabled)
            return;

        for (var slot = 0; slot < Slots; slot++)
            Run(slot);
    }
}
