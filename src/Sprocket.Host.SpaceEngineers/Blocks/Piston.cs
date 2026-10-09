using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyPistonBase is declared
// in both, and the mod-side one is the one with the events on it.
using PistonStatus = Sandbox.ModAPI.Ingame.PistonStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A piston: extended, retracted or moved to a position, and the members of its terminal said in
    /// Sprocket's words.
    /// </summary>
    /// <remarks>
    /// Moving to a position is the game's own: it puts a limit at the position and runs the piston at
    /// it, so the piston's limits are the game's to overwrite, and are. It has arrived when it has
    /// reached the limit it is running towards. Extending and retracting run it to whichever limit it
    /// already has, and are finished when the piston says it is out or in.
    /// <para>
    /// A stop that leaves a machine moving is not a stop, so calling off any of the three stops the
    /// piston where it stands.
    /// </para>
    /// </remarks>
    public sealed class Piston : Capability
    {
        /// <summary>Moving to a position, which is Sprocket's own and waits until it gets there.</summary>
        public const string MoveTo = "move-to";

        /// <summary>The action that extends the piston all the way.</summary>
        public const string Extend = "extend";

        /// <summary>The action that retracts the piston all the way.</summary>
        public const string Retract = "retract";

        const float Hair = 0.0001f;

        static readonly Reading Out = Reading.Of<IMyPistonBase>(piston => piston.Status == PistonStatus.Extended);

        static readonly Reading In = Reading.Of<IMyPistonBase>(piston => piston.Status == PistonStatus.Retracted);

        static readonly Reading Arrived = Reading.Of<IMyPistonBase>(AtLimit);

        // Extending and retracting are the game's own, performed as its.
        static readonly Doing Extends = Doing.Of<IMyPistonBase>(null, Out,
            piston => Why(piston, piston.Status == PistonStatus.Extended, 1), Stop, Outwards);

        static readonly Doing Retracts = Doing.Of<IMyPistonBase>(null, In,
            piston => Why(piston, piston.Status == PistonStatus.Retracted, -1), Stop, Inwards);

        static readonly Doing Moves = Doing.Of<IMyPistonBase, Move>(Move.Read, Moved, Arrived, null,
            (piston, move, seen) => Why(piston, AtLimit(piston), 0), Stop, Expected);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyPistonBase;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Extend, "extend", Extends, "Extend").Lasting().In(Category.Movement)
                .WithAction(Retract, "retract", Retracts, "Retract").Lasting().In(Category.Movement)
                .WithAction(MoveTo, "move", Moves, AskedPosition(block), AskedSpeed(block)).Lasting().Listed("move to")
                .In(Category.Movement)
                .Hiding("SetAndMove")
                .Renaming("Reverse", false, "reverse")
                .Renaming("Velocity", true, "speed")
                .Renaming("LowerLimit", true, "lower limit")
                .Renaming("UpperLimit", true, "upper limit");
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

        /// <summary>Where along its travel the piston will go, which is the piston's own to say.</summary>
        static Field AskedPosition(IMyTerminalBlock block)
        {
            var piston = block as IMyPistonBase;
            var least = piston == null ? 0f : piston.LowestPosition;
            var most = piston == null ? 10f : piston.HighestPosition;

            var position = Field.Between("position", Said(least), Said(most));
            position.Needed = true;
            position.Entry = Entry.Decimal;
            position.Lead = "to";
            position.Unit = " m";
            return position;
        }

        static Field AskedSpeed(IMyTerminalBlock block)
        {
            // A move at no speed sets out for a position it can never reach, so there is no reading of an
            // empty speed that means anything.
            var piston = block as IMyPistonBase;
            var speed = Field.Between("speed", "0", Said(piston == null ? 5f : piston.MaxVelocity));
            speed.Needed = true;
            speed.Above = true;
            speed.Entry = Entry.Decimal;
            speed.Default = "1";
            speed.Lead = "at";
            speed.Unit = " m/s";
            return speed;
        }

        /// <summary>A bound as a field writes one: whole where it is whole.</summary>
        static string Said(float number)
        {
            return number == (float)Math.Floor(number)
                ? ((int)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString("0.##", CultureInfo.InvariantCulture);
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

        /// <summary>Moves the piston to the position a step gave it.</summary>
        static void Moved(IMyPistonBase piston, Move move, Recall seen)
        {
            if (!move.Given)
                return;

            if (Math.Abs(piston.CurrentPosition - move.Position) <= float.Epsilon)
                Hold(piston, move.Position, move.Speed);
            else
                piston.MoveToPosition(move.Position, move.Speed);
        }

        /// <summary>
        /// Leaves the piston as a finished move to where it stands would: the limit at the position, and
        /// the speed pushing against it.
        /// </summary>
        /// <remarks>
        /// The game does nothing at all when told to move to where a piston already is, so a piston not
        /// already pushing that way would never read as arrived - and a sequence that moves something to
        /// where it last left it is run every time it cycles.
        /// </remarks>
        static void Hold(IMyPistonBase piston, float position, float speed)
        {
            piston.MaxLimit = position;
            piston.Velocity = speed;
        }

        /// <summary>Stops the piston where it stands, since a stop that leaves a machine moving is not a stop.</summary>
        static void Stop(IMyPistonBase piston)
        {
            piston.Velocity = 0f;
        }

        /// <summary>
        /// Why the piston is not getting where it was sent, or nothing while it still might: <paramref name="arrived" /> where
        /// it is there, and heading out where <paramref name="sense" /> is positive, in where negative, and either way at nought.
        /// </summary>
        /// <remarks>
        /// Whether it can move at all, then whether it is still heading the right way. Its speed was set
        /// by what sent it, so a speed of nought or the wrong way round means something else has been at
        /// it and nothing is heading there any more.
        /// </remarks>
        static Trouble Why(IMyPistonBase piston, bool arrived, int sense)
        {
            if (arrived)
                return Trouble.None;

            if (!piston.IsWorking)
                return Trouble.Waiting(Cause.PistonNoPower);

            if (!piston.IsAttached)
                return Trouble.Waiting(Cause.PistonNoHead);

            if (piston.Velocity == 0f)
                return Trouble.Fatal(Cause.PistonStopped);

            if (sense > 0 && piston.Velocity < 0f)
                return Trouble.Fatal(Cause.PistonReversedExtending);

            if (sense < 0 && piston.Velocity > 0f)
                return Trouble.Fatal(Cause.PistonReversedRetracting);

            return Trouble.None;
        }

        /// <summary>The ticks extending should take: the distance to its upper limit, at the speed it goes.</summary>
        /// <remarks>Its speed is in metres a second, and the game runs sixty ticks to the second.</remarks>
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
        static int Expected(IMyPistonBase piston, Move move)
        {
            if (!move.Given)
                return GridEntity.Unestimated;

            return Ticks(Math.Abs(move.Position - piston.CurrentPosition), move.Speed);
        }

        static int Ticks(float distance, float speed)
        {
            return speed <= 0f ? GridEntity.Unestimated : (int)Math.Ceiling(Math.Max(0f, distance) / speed * 60f);
        }

        /// <summary>Where a step told the piston to move to, and how fast.</summary>
        sealed class Move
        {
            /// <summary>Whether the step gave both; a step that did not moves nothing.</summary>
            public bool Given;

            public float Position;

            public float Speed;

            /// <summary>The position and speed a step moving the piston was given, in that order.</summary>
            public static Move Read(IList<Literal> arguments)
            {
                var move = new Move();
                if (arguments == null || arguments.Count < 2)
                    return move;

                move.Position = (float)arguments[0].Real;
                move.Speed = (float)Math.Abs(arguments[1].Real);
                move.Given = true;
                return move;
            }
        }
    }
}
