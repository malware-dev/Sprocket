using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.Sim.Machine;

/// <summary>A wheel suspension on the rig, its wheel moved along its travel towards the height it is set to.</summary>
/// <remarks>
/// The suspension stands at the world's origin facing the world's forward, and its wheel moves along that forward at a fixed
/// pace until it reaches the height, which is how far a ship it carries rises. Load, springs and the ground are not here, so
/// a wheel on the rig always arrives. The heights are a large 3x3 suspension's, -1.5 to 1.3 meters.
/// </remarks>
public sealed class Suspension : Fitting
{
    const float Lowest = -1.5f;
    const float Highest = 1.3f;

    // How far the wheel moves in a tick: a meter a second, about the 1.1 m/s a loaded ship rose at (WheelHeightGameTest).
    const float Pace = 1f / 60f;

    float _height;
    float _travel;

    /// <summary>A suspension, <paramref name="id" /> and <paramref name="name" />, of the game's type and subtype, called <paramref name="display" />.</summary>
    public Suspension(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        var wheel = Pretend.To<IMyAttachableTopBlock>()
            .Does("GetPosition", () => Vector3D.Forward * _travel)
            .Done();

        Fit(Pretend.To<IMyMotorSuspension>()
            .Gives("Height", () => _height)
            .Takes("Height", value => _height = Math.Max(Lowest, Math.Min(Highest, (float)value)))
            .Gives("IsAttached", () => Attached)
            .Gives("Top", () => Attached ? wheel : null)
            .Gives("WorldMatrix", () => MatrixD.Identity)
            .Does("GetPosition", () => Vector3D.Zero));

        Offering("Add Top Part", "Add Wheel");
        Carrying("Height", "Height", ValueKind.Real, "-1.5", "1.3");
    }

    /// <summary>Whether it has a wheel on it, without which nothing moves.</summary>
    public bool Attached { get; set; } = true;

    /// <summary>How far along its travel the wheel stands, in meters.</summary>
    public float Travel => _travel;

    /// <inheritdoc />
    public override void Tick()
    {
        if (!Attached)
            return;

        var gap = _height - _travel;
        _travel += Math.Sign(gap) * Math.Min(Math.Abs(gap), Pace);
    }
}
