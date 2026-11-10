using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A railgun on the rig: a launcher carrying a capacitor, which is what makes the game's launcher a railgun.</summary>
public sealed class Railgun : Fitting
{
    /// <summary>A railgun known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Railgun(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMySmallMissileLauncherReload>());
        Holding<IMyEntityCapacitorComponent>(Capacitor);
    }

    /// <summary>The capacitor the railgun charges, empty and with no power to charge it.</summary>
    public Capacitor Capacitor { get; } = new Capacitor();
}
