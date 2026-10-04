using System;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using MyRotationDirection = Sandbox.ModAPI.Ingame.MyRotationDirection;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A rotor on the rig, turned by its speed until it meets a limit.
/// </summary>
/// <remarks>
/// Modelled on the game's own rules, which the mod reads rather than repeats: turning to an angle puts
/// one limit at the angle, takes the other away and sets the speed towards it, and a rotor given
/// limits has its angle said again by whole turns so that it lies within them. Everything else a
/// rotor does - torque, load, what it is carrying - is not here, so a rotor on the rig always
/// arrives.
/// <para>
/// A hinge is the same block with a subtype that says so, as in the game: a quarter turn either way
/// at most, its own lock and its own head.
/// </para>
/// </remarks>
sealed class Rotor : Fitting
{
    const float Short = 0.00005f;

    float _angle;
    float _lower = float.NegativeInfinity;
    float _upper = float.PositiveInfinity;
    float _speed;
    readonly float _reach;

    public Rotor(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        var hinge = subtype.Contains("Hinge");
        _reach = hinge ? 90f : 360f;
        var locks = hinge ? "HingeLock" : "RotorLock";

        Fit(Pretend.To<IMyMotorStator>()
            .Gives("Angle", () => _angle)
            .Gives("LowerLimitRad", () => _lower)
            .Gives("UpperLimitRad", () => _upper)
            .Gives("TargetVelocityRPM", () => _speed)
            .Gives("MaxRotorAngularVelocity", () => 30f * 2f * (float)Math.PI / 60f)
            .Gives("IsAttached", () => Attached)
            .Gives("TopGrid", () => TopGrid)
            .Gives("RotorLock", () => Locked)
            .Gives("Torque", () => Torque)
            .Takes("LowerLimitRad", value => _lower = (float)value)
            .Takes("UpperLimitRad", value => _upper = (float)value)
            .Takes("TargetVelocityRPM", value => _speed = (float)value)
            .Does("RotateToAngle", arguments =>
            {
                RotateTo((MyRotationDirection)arguments[0], (float)arguments[1], (float)arguments[2]);
                Told("rotate " + arguments[1] + " at " + arguments[2]);
            }));

        if (hinge)
        {
            Offering("AddHingeTopPart", "Add Hinge Head");
        }
        else
        {
            Offering("AddRotorTopPart", "Add Rotor Head");
            Offering("AddSmallRotorTopPart", "Add Small Rotor Head");
            Offering("AddMediumRotorTopPart", "Add Medium Rotor Head");
        }

        Offering("Reverse", "Reverse");
        Offering("Attach", "Attach");
        Offering("Detach", "Detach");
        Offering(locks + "_On", hinge ? "Hinge lock On" : "Rotor lock On");
        Offering(locks + "_Off", hinge ? "Hinge lock Off" : "Rotor lock Off");
        Offering("RotateToAngle", "Rotate To Angle", 3);
        Carrying(locks, hinge ? "Hinge lock" : "Rotor lock", ValueKind.Flag);
        Carrying("Velocity", "Velocity", ValueKind.Real, "-30", "30");
        Carrying("LowerLimit", "Lower limit", ValueKind.Real, "-361", "360");
        Carrying("UpperLimit", "Upper limit", ValueKind.Real, "-360", "361");
        Carrying("Torque", "Torque", ValueKind.Real, "0", "33600000");
        Carrying("BrakingTorque", "Braking torque", ValueKind.Real, "0", "33600000");
        Carrying("Displacement", "Rotor displacement", ValueKind.Real, "-0.4", "0.2");
    }

    /// <summary>How fast it is turning, in revolutions a minute, and which way.</summary>
    public float Speed => _speed;

    /// <summary>Whether it has a head on it, without which it turns nothing.</summary>
    public bool Attached { get; set; } = true;

    /// <summary>The grid its head stands on, or null where it has none.</summary>
    public IMyCubeGrid TopGrid { get; set; }

    /// <summary>Whether its lock is on, which holds it where it stands.</summary>
    public bool Locked { get; set; }

    /// <summary>How hard it turns, which is nothing when it is nought.</summary>
    public float Torque { get; set; } = 1000f;

    /// <summary>
    /// Whether something is holding it, so its angle stays where it is however fast it is told to go.
    /// </summary>
    /// <remarks>
    /// What a rotor leaning on a wall does. Nothing about the block says so - the speed and the
    /// torque read the same as a rotor turning freely - which is the whole reason the mod watches
    /// the angle.
    /// </remarks>
    public bool Held { get; set; }

    /// <summary>The rotor's angle in degrees, nought to three hundred and sixty.</summary>
    public float Degrees
    {
        get
        {
            var degrees = _angle * 180f / (float)Math.PI % 360f;
            return degrees < 0 ? degrees + 360f : degrees;
        }
    }

    /// <summary>Stands the rotor at an angle, without its having turned there.</summary>
    public void Stand(float degrees)
    {
        _angle = degrees * (float)Math.PI / 180f;
    }

    /// <summary>What the game does when a rotor is told to turn to an angle.</summary>
    void RotateTo(MyRotationDirection way, float degrees, float rpm)
    {
        var target = Math.Max(-_reach, Math.Min(_reach, degrees)) * (float)Math.PI / 180f;
        var difference = target - _angle;
        if (Math.Abs(difference) <= float.Epsilon)
            return;

        if (way == MyRotationDirection.AUTO)
            way = Math.Sin(difference) > 0 ? MyRotationDirection.CW : MyRotationDirection.CCW;

        if (target < 0)
            target += 2 * (float)Math.PI;

        _lower = float.NegativeInfinity;
        _upper = float.PositiveInfinity;
        if (way == MyRotationDirection.CW)
        {
            _upper = target;
            _speed = Math.Abs(rpm);
        }
        else
        {
            _lower = target;
            _speed = -Math.Abs(rpm);
        }

        Within();
    }

    /// <summary>Says the angle again by whole turns so it lies within the limits, as the game does.</summary>
    void Within()
    {
        var turn = 2 * (float)Math.PI;
        while (_angle > _upper)
            _angle -= turn;
        while (_angle < _lower)
            _angle += turn;
    }

    /// <inheritdoc />
    public override void Tick()
    {
        if (!Enabled || Held || Locked || !Attached || Torque <= 0f || _speed == 0)
            return;

        // A revolution a minute is a sixtieth of a turn a second, at sixty ticks to the second.
        _angle += _speed * 2 * (float)Math.PI / 3600f;

        // Held against a limit it rests a hair short of it rather than on it, as a rotor held by a
        // constraint does, so the mod's test for having arrived has to allow for that.
        _angle = Math.Max(_lower + Short, Math.Min(_upper - Short, _angle));
    }
}
