using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A wheel suspension: the members of its terminal said in Sprocket's words, where the wheel is turned, and whether it has a wheel.</summary>
    public sealed partial class Suspension : Capability
    {
        const string Steered = "A suspension raises nothing as its wheel turns.";

        static readonly Reading Wheeled = Reading.Of<IMyMotorSuspension>(suspension => suspension.IsAttached);

        // The game's steering angle is in radians; the terminal says degrees.
        static readonly OwnValue Turned = OwnValue.Number<IMyMotorSuspension>(suspension => suspension.SteerAngle * 180.0 / Math.PI);

        static readonly OwnValue Attached = OwnValue.Flag(Wheeled);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMotorSuspension;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("steering_angle", "steering angle", Turned).Measured("°").Looked(Steered).In(Category.Movement)
                .WithValue("wheel_attached", "wheel attached", Attached).Announced().In(Category.Repair)
                .Hiding("ResetMaxSteerAngle", "ResetHeight", "ResetSpeed Limit", "ResetPropulsion override",
                    "ResetSteer override")
                .Renaming("Propulsion override", true, "propulsion override")
                .Measuring("Propulsion override", " %", 100)
                .Renaming("Steer override", true, "steer override")
                .Measuring("Steer override", " %", 100)
                .Renaming("Speed Limit", true, "speed limit")
                .Measuring("Speed Limit", " km/h")
                .Renaming("Power", true, "wheel power")
                .Measuring("Power", " %")
                .Renaming("Braking", true, "brake")
                .Renaming("Braking", false, "toggle brake")
                .Renaming("Steering", true, "steering")
                .Renaming("Steering", false, "toggle steering")
                .Renaming("Propulsion", true, "propulsion")
                .Renaming("Propulsion", false, "toggle propulsion")
                .Renaming("InvertSteering", true, "invert steering")
                .Renaming("InvertSteering", false, "toggle invert steering")
                .Renaming("InvertPropulsion", true, "invert propulsion")
                .Renaming("InvertPropulsion", false, "toggle invert propulsion")
                .Renaming("MaxSteerAngle", true, "maximum steering angle")
                .Measuring("MaxSteerAngle", "°")
                .Renaming("EnableParking", true, "parking brake")
                .Renaming("EnableParking", false, "toggle parking brake")
                .Renaming("Height", true, "height")
                .Measuring("Height", " m")
                .Renaming("Strength", true, "strength")
                .Measuring("Strength", " %")
                .Renaming("Friction", true, "friction")
                .Measuring("Friction", " %")
                .Renaming("AirShock", true, "air shock")
                .Renaming("AirShock", false, "toggle air shock")
                .Renaming("Add Top Part", false, "add wheel")
                .Renaming("SafetyDetach", true, "safety detach distance")
                .Renaming("ShareInertiaTensor", true, "share inertia tensor")
                .Renaming("ShareInertiaTensor", false, "toggle share inertia tensor");
        }

        /// <summary>Told as the wheel is attached or comes off.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var suspension = block as IMyMotorSuspension;
            return suspension == null ? null : new Listening(suspension, told);
        }
    }
}
