using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using Sandbox.Definitions;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A wheel suspension: the members of its terminal said in Sprocket's words, where the wheel is turned, whether it has a wheel, and moving the wheel to a height.</summary>
    public sealed partial class Suspension : Capability
    {
        /// <summary>Moving the wheel to a height, which is Sprocket's own and waits until the wheel stops moving.</summary>
        public const string MoveTo = "move_to";

        const string Steered = "A suspension raises nothing as its wheel turns.";

        static readonly Reading Wheeled = Reading.Of<IMyMotorSuspension>(suspension => suspension.IsAttached);

        // The game's steering angle is in radians; the terminal says degrees.
        static readonly OwnValue Turned = OwnValue.Real<IMyMotorSuspension>(suspension => suspension.SteerAngle * 180.0 / Math.PI);

        static readonly OwnValue Attached = OwnValue.Flag(Wheeled);

        static readonly Reading Settled = Reading.Remembering<IMyMotorSuspension>((suspension, seen) =>
            suspension.IsAttached && !WheelMotion.Moving(suspension, seen));

        static readonly OwnAction Moves = OwnAction.Of<IMyMotorSuspension, SuspensionMove>(SuspensionMove.Read, Moved, Settled, null,
            (suspension, move, seen) => Why(suspension));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMotorSuspension;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(MoveTo, "move", Moves, AskedHeight(block)).Lasting().Listed("move to").In(Category.Movement)
                .WithValue("steering_angle", "steering angle", Turned).Measured("°").Looked(Steered).In(Category.Movement)
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

        /// <summary>Where along its travel the wheel will go, between the heights the suspension allows.</summary>
        static Field AskedHeight(IMyTerminalBlock block)
        {
            // Without a definition, a large 3x3 suspension's (CubeBlocks_Wheels.sbc, MinHeight and MaxHeight).
            var definition = block == null ? null : MyDefinitionManager.Static?.GetCubeBlockDefinition(block.BlockDefinition) as MyMotorSuspensionDefinition;
            var least = definition?.MinHeight ?? -1.5f;
            var most = definition?.MaxHeight ?? 1.3f;

            var height = Field.Between("height", Field.Bound(least), Field.Bound(most));
            height.Needed = true;
            height.Entry = FieldInput.Real;
            height.Lead = "to";
            height.Unit = " m";
            return height;
        }

        /// <summary>Sets the wheel's height to the one a step gave it, and starts watching it move.</summary>
        static void Moved(IMyMotorSuspension suspension, SuspensionMove move, ReaderMemory seen)
        {
            if (!move.Given)
                return;

            suspension.Height = move.Height;
            WheelMotion.Began(suspension, seen);
        }

        /// <summary>Why the wheel is not getting where it was sent, or none while it might.</summary>
        static BlockTrouble Why(IMyMotorSuspension suspension)
        {
            return suspension.IsAttached ? BlockTrouble.None : BlockTrouble.Waiting(Cause.WheelNoWheel);
        }

        /// <summary>Told as the wheel is attached or comes off.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var suspension = block as IMyMotorSuspension;
            return suspension == null ? null : new Listening(suspension, told);
        }
    }
}
