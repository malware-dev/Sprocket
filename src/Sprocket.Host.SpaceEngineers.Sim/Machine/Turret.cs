using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A gatling turret on the rig: whether it has a target, is on it, is firing and has a person at its controls, as a test sets them.</summary>
/// <remarks>
/// Nothing aims and nothing fires: what it reads is what a test said, and resetting its targeting is written down. It is not
/// the game's gun object, so it has no rounds to count.
/// </remarks>
public sealed class Turret : Fitting
{
    /// <summary>A turret, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Turret(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyLargeGatlingTurret>()
            .Gives("HasTarget", () => Targeting)
            .Gives("IsAimed", () => OnTarget)
            .Gives("IsUnderControl", () => Controlled)
            .Gives("IsShooting", () => Firing)
            .Does("ResetTargetingToDefault", () =>
            {
                Told("reset targeting");
                return null!;
            }));
    }

    /// <summary>Whether it reads as having a target.</summary>
    public bool Targeting { get; set; }

    /// <summary>Whether it reads as on its target.</summary>
    public bool OnTarget { get; set; }

    /// <summary>Whether it reads as having a person at its controls.</summary>
    public bool Controlled { get; set; }

    /// <summary>Whether it reads as firing.</summary>
    public bool Firing { get; set; }
}
