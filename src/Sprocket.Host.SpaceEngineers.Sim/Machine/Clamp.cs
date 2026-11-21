using System;
using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;

namespace Sprocket.Sim.Machine;

/// <summary>A landing gear or magnetic plate on the rig, which a test locks and unlocks as the game would.</summary>
/// <remarks>Announces each change of its lock mode through <c>LockModeChanged</c>, with the mode it had, as the game does.</remarks>
public sealed class Clamp : Fitting
{
    LandingGearMode _mode = LandingGearMode.Unlocked;
    Action<IMyLandingGear, LandingGearMode>? _changed;

    /// <summary>An unlocked gear, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Clamp(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        var gear = Pretend.To<IMyLandingGear>()
            .Gives("LockMode", () => _mode)
            .Gives("IsLocked", () => _mode == LandingGearMode.Locked)
            .Does("add_LockModeChanged", handler => { _changed += (Action<IMyLandingGear, LandingGearMode>)handler[0]; })
            .Does("remove_LockModeChanged", handler => { _changed -= (Action<IMyLandingGear, LandingGearMode>)handler[0]; });

        Fit(gear);
    }

    /// <summary>Puts the gear in <paramref name="mode" />, announcing the change as the game does.</summary>
    public void Lock(LandingGearMode mode)
    {
        if (_mode == mode)
            return;

        var was = _mode;
        _mode = mode;
        _changed?.Invoke((IMyLandingGear)Block, was);
    }
}
