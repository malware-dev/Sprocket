using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A wheel suspension: the members of its terminal said in Sprocket's words, where the wheel is turned, and whether it has a wheel.</summary>
    public sealed class Suspension : Capability
    {
        const string Steered = "A suspension raises nothing as its wheel turns.";

        static readonly Reading Wheeled = Reading.Of<IMyMotorSuspension>(suspension => suspension.IsAttached);

        // The game's steering angle is in radians; the terminal says degrees.
        static readonly Being Turned = Being.Number<IMyMotorSuspension>(suspension => suspension.SteerAngle * 180.0 / Math.PI);

        static readonly Being Attached = Being.Flag(Wheeled);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMotorSuspension;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("steering-angle", "steering angle", Turned).Looked(Steered).In(Category.Movement)
                .WithValue("wheel-attached", "wheel attached", Attached).Announced()
                .Hiding("ResetMaxSteerAngle", "ResetHeight", "ResetSpeed Limit", "ResetPropulsion override",
                    "ResetSteer override")
                .Renaming("Propulsion override", true, "propulsion override")
                .Renaming("Steer override", true, "steering override")
                .Renaming("Speed Limit", true, "speed limit")
                .Renaming("Power", true, "wheel power")
                .Renaming("Braking", true, "reacting to braking")
                .Renaming("Braking", false, "toggle reacting to braking")
                .Renaming("Steering", true, "steering")
                .Renaming("Steering", false, "toggle steering")
                .Renaming("Propulsion", true, "propulsion")
                .Renaming("Propulsion", false, "toggle propulsion")
                .Renaming("InvertSteering", true, "steering inverted")
                .Renaming("InvertSteering", false, "toggle steering inverted")
                .Renaming("InvertPropulsion", true, "propulsion inverted")
                .Renaming("InvertPropulsion", false, "toggle propulsion inverted")
                .Renaming("MaxSteerAngle", true, "max steering angle")
                .Renaming("EnableParking", true, "used for parking")
                .Renaming("EnableParking", false, "toggle used for parking")
                .Renaming("Height", true, "height")
                .Renaming("Strength", true, "strength")
                .Renaming("Friction", true, "friction")
                .Renaming("AirShock", true, "air shock")
                .Renaming("AirShock", false, "toggle air shock")
                .Renaming("Add Top Part", false, "add wheel")
                .Renaming("SafetyDetach", true, "safety detach distance")
                .Renaming("ShareInertiaTensor", true, "sharing inertia tensor")
                .Renaming("ShareInertiaTensor", false, "toggle sharing inertia tensor");
        }

        /// <summary>Told as the wheel is attached or comes off, which every suspension raises.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var suspension = block as IMyMotorSuspension;
            return suspension == null ? null : new Listening(suspension, told);
        }

        /// <summary>A suspension being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyMotorSuspension _suspension;
            readonly Action _told;

            public Listening(IMyMotorSuspension suspension, Action told)
            {
                _suspension = suspension;
                _told = told;
                _suspension.OnAttachedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _suspension.OnAttachedChanged -= Changed;
            }

            void Changed(IMyMechanicalConnectionBlock block)
            {
                _told();
            }
        }
    }
}
