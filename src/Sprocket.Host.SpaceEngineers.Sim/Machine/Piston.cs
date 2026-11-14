using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using PistonStatus = Sandbox.ModAPI.Ingame.PistonStatus;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A piston on the rig, driven along its travel by its speed until it meets a limit.
/// </summary>
/// <remarks>
/// Modelled on the game's own rules, which the mod reads rather than repeats: extending and
/// retracting turn the speed out or in, and moving to a position puts a limit at the position and the
/// speed towards it. Load, what it is pushing and how hard are not here, so a piston on the rig always
/// arrives. The travel is a large piston's, nought to ten metres at up to five a second.
/// </remarks>
public sealed class Piston : Fitting
{
    const float Lowest = 0f;
    const float Highest = 10f;
    const float Fastest = 5f;

    float _position;
    float _lower = Lowest;
    float _upper = Highest;
    float _speed = 0.5f;
    Action<bool>? _reached;

    /// <summary>A piston, <paramref name="id" /> and <paramref name="name" />, of the game's type and subtype, called <paramref name="display" />.</summary>
    public Piston(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        // No top: the rig's pistons carry nothing, so there is no grid on the other side.
        Fit(Pretend.To<IMyExtendedPistonBase>()
            .Gives("TopGrid", () => null)
            .Gives("CurrentPosition", () => _position)
            .Gives("NormalizedPosition", () => (_position - Lowest) / (Highest - Lowest))
            .Gives("LowestPosition", () => Lowest)
            .Gives("HighestPosition", () => Highest)
            .Gives("MaxVelocity", () => Fastest)
            .Gives("MinLimit", () => _lower)
            .Gives("MaxLimit", () => _upper)
            .Gives("Velocity", () => _speed)
            .Gives("Status", () => Status)
            .Gives("IsAttached", () => Attached)
            .Does("add_LimitReached", handler => { _reached += (Action<bool>)handler[0]; })
            .Does("remove_LimitReached", handler => { _reached -= (Action<bool>)handler[0]; })
            .Takes("MinLimit", value => _lower = Clamped((float)value))
            .Takes("MaxLimit", value => _upper = Clamped((float)value))
            .Takes("Velocity", value => _speed = Math.Max(-Fastest, Math.Min(Fastest, (float)value)))
            .Does("Extend", _ => Out())
            .Does("Retract", _ => In())
            .Does("Reverse", _ => _speed = -_speed)
            .Does("MoveToPosition", arguments =>
            {
                MoveTo((float)arguments[0], (float)arguments[1]);
                Told("move " + arguments[0] + " at " + arguments[1]);
            }));

        Offering("Add Top Part", "Add Piston Head");
        Offering("Reverse", "Reverse");
        Offering("Extend", "Extend");
        Offering("Retract", "Retract");
        Offering("SetAndMove", "Set And Move", 2);
        Carrying("Velocity", "Velocity", ValueKind.Real, "-5", "5");
        Carrying("UpperLimit", "Maximum distance", ValueKind.Real, "0", "10");
        Carrying("LowerLimit", "Minimum distance", ValueKind.Real, "0", "10");
    }

    /// <summary>Where along its travel it is, in metres.</summary>
    public float Position => _position;

    /// <summary>How fast it is moving, in metres a second, and which way.</summary>
    public float Speed => _speed;

    /// <summary>Whether it has a head on it, without which it moves nothing.</summary>
    public bool Attached { get; set; } = true;

    /// <summary>Whether something is holding it, so its position stays where it is however fast it is told to go.</summary>
    /// <remarks>What a piston pushing a drill into rock it cannot cut does. Nothing about the block says so, which is why the mod watches the position.</remarks>
    public bool Held { get; set; }

    /// <summary>Stands the piston at a position, without its having moved there.</summary>
    public void Stand(float metres)
    {
        _position = Clamped(metres);
    }

    /// <summary>What the game reports: moving one way or the other, or resting at the end it was going to.</summary>
    PistonStatus Status
    {
        get
        {
            if (_speed > 0)
                return _position >= _upper ? PistonStatus.Extended : PistonStatus.Extending;

            if (_speed < 0)
                return _position <= _lower ? PistonStatus.Retracted : PistonStatus.Retracting;

            return PistonStatus.Stopped;
        }
    }

    static float Clamped(float metres)
    {
        return Math.Max(Lowest, Math.Min(Highest, metres));
    }

    void Out()
    {
        _speed = Math.Abs(_speed);
        Told("extend");
    }

    void In()
    {
        _speed = -Math.Abs(_speed);
        Told("retract");
    }

    /// <summary>What the game does when a piston is told to move to a position.</summary>
    void MoveTo(float position, float speed)
    {
        var target = Clamped(position);
        if (_position < target)
        {
            _upper = target;
            _speed = Math.Abs(speed);
        }
        else if (_position > target)
        {
            _lower = target;
            _speed = -Math.Abs(speed);
        }
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "Extend")
            Out();
        else if (actionId == "Retract")
            In();
        else if (actionId == "Reverse")
            _speed = -_speed;
        else
            base.Perform(actionId, arguments);
    }

    /// <summary>How many are listening for the piston to reach either end.</summary>
    public int Listeners => _reached == null ? 0 : _reached.GetInvocationList().Length;

    /// <inheritdoc />
    /// <remarks>Raises <c>LimitReached</c> on the tick it arrives at the limit it is moving towards, as the game does.</remarks>
    public override void Tick()
    {
        if (!Enabled || !Attached || Held || _speed == 0)
            return;

        var was = _position;
        _position = Math.Max(_lower, Math.Min(_upper, _position + _speed / 60f));

        var limit = _speed > 0 ? _upper : _lower;
        if (_position == limit && was != limit)
            _reached?.Invoke(_speed > 0);
    }
}
