using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyPistonBase is declared
// in both, and the mod-side one is the one with the events on it.
using PistonStatus = Sandbox.ModAPI.Ingame.PistonStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A piston, extended, retracted or moved to a position.</summary>
    public sealed partial class Piston : Capability
    {
        /// <summary>Moving to a position, which is Sprocket's own and waits until it gets there.</summary>
        public const string MoveTo = "move_to";

        /// <summary>The action that extends the piston all the way.</summary>
        public const string Extend = "extend";

        /// <summary>The action that retracts the piston all the way.</summary>
        public const string Retract = "retract";

        const float Hair = 0.0001f;

        const string Moving = "A piston raises its position on every tick it moves, too often to listen to, and nothing else as it starts or stops.";

        static readonly string[] Statuses = { "extended", "retracted", "extending", "retracting", "stopped" };

        static readonly OwnValue State = OwnValue.OneOf<IMyPistonBase>(Statuses, piston => Rank(piston.Status));

        static readonly OwnValue Travelled = OwnValue.Number<IMyPistonBase>(piston => piston.CurrentPosition);

        static readonly Reading Out = Reading.Of<IMyPistonBase>(piston => piston.Status == PistonStatus.Extended);

        static readonly Reading In = Reading.Of<IMyPistonBase>(piston => piston.Status == PistonStatus.Retracted);

        static readonly Reading Arrived = Reading.Of<IMyPistonBase>(AtLimit);

        static readonly OwnAction Extends = OwnAction.Watched<IMyPistonBase>(null, Out,
            (piston, seen) => Why(piston, piston.Status == PistonStatus.Extended, 1, seen), Stop, Outwards);

        static readonly OwnAction Retracts = OwnAction.Watched<IMyPistonBase>(null, In,
            (piston, seen) => Why(piston, piston.Status == PistonStatus.Retracted, -1, seen), Stop, Inwards);

        static readonly OwnAction Moves = OwnAction.Of<IMyPistonBase, PistonMove>(PistonMove.Read, Moved, Arrived, null,
            (piston, move, seen) => Why(piston, AtLimit(piston), 0, seen), Stop, Expected);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyPistonBase;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Extend, "extend", Extends, "Extend").LastingAnnounced().In(Category.Movement)
                .WithAction(Retract, "retract", Retracts, "Retract").LastingAnnounced().In(Category.Movement)
                .WithAction(MoveTo, "move", Moves, AskedPosition(block), AskedSpeed(block)).LastingAnnounced().Listed("move to")
                .In(Category.Movement)
                .Hiding("SetAndMove", "ResetVelocity")
                .Renaming("Reverse", false, "reverse")
                .Renaming("Velocity", true, "velocity")
                .Measuring("Velocity", " m/s")
                .Renaming("LowerLimit", true, "minimum distance")
                .Measuring("LowerLimit", " m")
                .Renaming("UpperLimit", true, "maximum distance")
                .Measuring("UpperLimit", " m")
                .Renaming("MaxImpulseAxis", true, "maximum impulse along the axis")
                .Measuring("MaxImpulseAxis", " N")
                .Renaming("MaxImpulseNonAxis", true, "maximum impulse across the axis")
                .Measuring("MaxImpulseNonAxis", " N")
                .Renaming("Add Top Part", false, "add piston head")
                .Renaming("SafetyDetach", true, "safety detach distance")
                .Measuring("SafetyDetach", " m")
                .Renaming("ShareInertiaTensor", true, "share inertia tensor")
                .Renaming("ShareInertiaTensor", false, "toggle share inertia tensor");

            // Where it is, and whether it is going anywhere, change as it moves, which it says nothing of; it says when it
            // reaches either end.
            kind.WithValue("position", "position", Travelled).Measured(" m").Looked(Moving).In(Category.Movement)
                .WithValue("status", "status", State).Status().Looked(Moving).In(Category.Movement)
                .WithChange("finishes_extending", "finishes extending", "status", Literal.Name("extended")).Announced()
                .Reading("is extended", "is not extended")
                .WithChange("finishes_retracting", "finishes retracting", "status", Literal.Name("retracted")).Announced()
                .Reading("is retracted", "is not retracted");
        }

        /// <summary>Told as the piston reaches either end of its travel.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var piston = block as IMyPistonBase;
            return piston == null ? null : new Listening(piston, told);
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var piston = block as IMyPistonBase;
            if (piston == null)
                return Status.None;

            switch (piston.Status)
            {
                case PistonStatus.Extended: return Status.Of(Condition.Extended);
                case PistonStatus.Retracted: return Status.Of(Condition.Retracted);
                case PistonStatus.Extending: return Status.Of(Condition.Extending);
                case PistonStatus.Retracting: return Status.Of(Condition.Retracting);
            }

            return Status.None;
        }

        /// <inheritdoc />
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var piston = block as IMyPistonBase;
            return piston != null
                && (piston.Status == PistonStatus.Extending || piston.Status == PistonStatus.Retracting);
        }

        /// <summary>Where <paramref name="status" /> stands among the piston's statuses.</summary>
        static int Rank(PistonStatus status)
        {
            switch (status)
            {
                case PistonStatus.Extended:
                    return 0;
                case PistonStatus.Retracted:
                    return 1;
                case PistonStatus.Extending:
                    return 2;
                case PistonStatus.Retracting:
                    return 3;
            }

            return 4;
        }

        /// <summary>Where along its travel the piston will go, which is the piston's own to say.</summary>
        static Field AskedPosition(IMyTerminalBlock block)
        {
            var piston = block as IMyPistonBase;
            var least = piston?.LowestPosition ?? 0f;
            var most = piston?.HighestPosition ?? 10f;

            var position = Field.Between("position", Field.Bound(least), Field.Bound(most));
            position.Needed = true;
            position.Entry = FieldInput.Decimal;
            position.Lead = "to";
            position.Unit = " m";
            return position;
        }

        static Field AskedSpeed(IMyTerminalBlock block)
        {
            // A move at no speed sets out for a position it can never reach, so there is no reading of an
            // empty speed that means anything.
            var piston = block as IMyPistonBase;
            var speed = Field.Between("speed", "0", Field.Bound(piston?.MaxVelocity ?? 5f));
            speed.Needed = true;
            speed.Above = true;
            speed.Entry = FieldInput.Decimal;
            speed.Default = "1";
            speed.Lead = "at";
            speed.Unit = " m/s";
            return speed;
        }

        /// <summary>Whether the piston has reached the limit it is running towards.</summary>
        /// <remarks>A piston that is not running is running towards nothing, and has arrived nowhere.</remarks>
        static bool AtLimit(IMyPistonBase piston)
        {
            if (piston.Velocity > 0)
                return piston.CurrentPosition > piston.MaxLimit - Hair;

            if (piston.Velocity < 0)
                return piston.CurrentPosition < piston.MinLimit + Hair;

            return false;
        }

        /// <summary>Moves the piston to the position a step gave it, and starts watching whether it is held.</summary>
        static void Moved(IMyPistonBase piston, PistonMove move, ReaderMemory seen)
        {
            if (!move.Given)
                return;

            if (Math.Abs(piston.CurrentPosition - move.Position) <= float.Epsilon)
                Hold(piston, move.Position, move.Speed);
            else
                piston.MoveToPosition(move.Position, move.Speed);

            MotionWatch.Began(piston.CurrentPosition, seen);
        }

        /// <summary>
        /// Leaves the piston as a finished move to where it stands would: the limit at the position, and
        /// the speed pushing against it.
        /// </summary>
        /// <remarks>The game does nothing when told to move a piston to where it already is.</remarks>
        static void Hold(IMyPistonBase piston, float position, float speed)
        {
            piston.MaxLimit = position;
            piston.Velocity = speed;
        }

        /// <summary>Stops the piston where it stands.</summary>
        static void Stop(IMyPistonBase piston)
        {
            piston.Velocity = 0f;
        }

        /// <summary>
        /// Why the piston is not getting where it was sent, or none while it might; <paramref name="sense" /> is positive outward and
        /// negative inward, and <paramref name="seen" /> holds where it stood when last looked at.
        /// </summary>
        static BlockTrouble Why(IMyPistonBase piston, bool arrived, int sense, ReaderMemory seen)
        {
            if (arrived)
                return BlockTrouble.None;

            if (!piston.IsWorking)
                return BlockTrouble.Waiting(Cause.PistonNoPower);

            if (!piston.IsAttached)
                return BlockTrouble.Waiting(Cause.PistonNoHead);

            if (piston.Velocity == 0f)
                return BlockTrouble.Fatal(Cause.PistonStopped);

            if (sense > 0 && piston.Velocity < 0f)
                return BlockTrouble.Fatal(Cause.PistonReversedExtending);

            if (sense < 0 && piston.Velocity > 0f)
                return BlockTrouble.Fatal(Cause.PistonReversedRetracting);

            // Waited through rather than given up on, as a held rotor is: whatever holds it may let go.
            return MotionWatch.Moving(piston.CurrentPosition, seen)
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.PistonHeld);
        }

        /// <summary>The ticks extending should take: the distance to its upper limit, at the speed it goes.</summary>
        /// <remarks>Its speed is in meters per second, and the game runs sixty ticks to the second.</remarks>
        static int Outwards(IMyPistonBase piston)
        {
            return Ticks(piston.MaxLimit - piston.CurrentPosition, Math.Abs(piston.Velocity));
        }

        /// <summary>The ticks retracting should take: the distance to its lower limit, at the speed it goes.</summary>
        static int Inwards(IMyPistonBase piston)
        {
            return Ticks(piston.CurrentPosition - piston.MinLimit, Math.Abs(piston.Velocity));
        }

        /// <summary>The ticks the move should take: the distance it has to go, at the speed it was given.</summary>
        static int Expected(IMyPistonBase piston, PistonMove move)
        {
            if (!move.Given)
                return GridEntity.Unestimated;

            return Ticks(Math.Abs(move.Position - piston.CurrentPosition), move.Speed);
        }

        static int Ticks(float distance, float speed)
        {
            return speed <= 0f ? GridEntity.Unestimated : (int)Math.Ceiling(Math.Max(0f, distance) / speed * 60f);
        }
    }
}
