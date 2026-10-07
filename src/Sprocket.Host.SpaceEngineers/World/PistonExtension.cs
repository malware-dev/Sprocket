using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Values;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyPistonBase is declared
// in both, and the mod-side one is the one with the events on it.
using PistonStatus = Sandbox.ModAPI.Ingame.PistonStatus;

namespace Sprocket.Host.SpaceEngineers.World
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
    /// </remarks>
    public sealed class PistonExtension : IBlockExtension
    {
        /// <summary>Moving to a position, which is Sprocket's own and waits until it gets there.</summary>
        public const string MoveTo = "move-to";

        /// <summary>The action that extends the piston all the way.</summary>
        public const string Extend = "extend";

        /// <summary>The action that retracts the piston all the way.</summary>
        public const string Retract = "retract";

        const float Hair = 0.0001f;

        const string Out = "extended";
        const string In = "retracted";
        const string Arrived = "arrived";
        const string IsOut = "is-extended";
        const string IsIn = "is-retracted";
        const string There = "is-there";

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyPistonBase;
        }

        /// <summary>Nothing to arrange: where it has got to is read off the piston.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Out, "extended")
                .WithProperty(In, "retracted")
                .WithProperty(Arrived, "at its position")
                .WithState(IsOut, Out, true, "is extended")
                .WithState(IsIn, In, true, "is retracted")
                .WithState(There, Arrived, true, "is at its position")
                .WithAction(Extend, "extend", IsOut, "Extend").Lasting().In(Category.Movement)
                .WithAction(Retract, "retract", IsIn, "Retract").Lasting().In(Category.Movement)
                .WithAction(MoveTo, "move", There, Position(block), Speed(block)).Lasting().Listed("move to")
                .In(Category.Movement)
                .Hiding("SetAndMove")
                .Renaming("Reverse", false, "reverse")
                .Renaming("Velocity", true, "speed")
                .Renaming("LowerLimit", true, "lower limit")
                .Renaming("UpperLimit", true, "upper limit");
        }

        /// <summary>Where along its travel the piston will go, which is the piston's own to say.</summary>
        static Field Position(IMyTerminalBlock block)
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

        static Field Speed(IMyTerminalBlock block)
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

        /// <inheritdoc />
        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var piston = block as IMyPistonBase;
            if (piston == null)
                return false;

            switch (propertyId)
            {
                case Out:
                    value = EntityValue.Of(piston.Status == PistonStatus.Extended);
                    return true;
                case In:
                    value = EntityValue.Of(piston.Status == PistonStatus.Retracted);
                    return true;
                case Arrived:
                    value = EntityValue.Of(AtLimit(piston));
                    return true;
            }

            return false;
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

        /// <summary>Moves the piston to a position; extending and retracting are the game's own, performed as its.</summary>
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var piston = block as IMyPistonBase;
            if (piston == null || actionId != MoveTo)
                return false;

            float position;
            float speed;
            if (!Given(values, out position, out speed))
                return true;

            if (Math.Abs(piston.CurrentPosition - position) <= float.Epsilon)
                Hold(piston, position, speed);
            else
                piston.MoveToPosition(position, speed);

            return true;
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

        /// <summary>The position and speed a step moving the piston was given, in that order.</summary>
        static bool Given(IList<Literal> values, out float position, out float speed)
        {
            position = 0;
            speed = 0;

            if (values == null || values.Count < 2)
                return false;

            position = (float)values[0].Real;
            speed = (float)Math.Abs(values[1].Real);
            return true;
        }

        /// <summary>Stops the piston where it stands, since a stop that leaves a machine moving is not a stop.</summary>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
            var piston = block as IMyPistonBase;
            if (piston != null && (actionId == MoveTo || actionId == Extend || actionId == Retract))
                piston.Velocity = 0f;
        }

        /// <inheritdoc />
        public bool Done(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return false;
        }

        /// <summary>
        /// Why the piston is not getting where it was sent, or nothing while it still might.
        /// </summary>
        /// <remarks>
        /// Whether it can move at all, then whether it is still heading the right way. Its speed was set
        /// by what sent it, so a speed of nought or the wrong way round means something else has been at
        /// it and nothing is heading there any more.
        /// </remarks>
        public Trouble TroubleWith(IMyTerminalBlock block, string actionId, IList<Literal> values, string stateId)
        {
            var piston = block as IMyPistonBase;
            if (piston == null)
                return Trouble.None;

            if (stateId == IsOut && piston.Status == PistonStatus.Extended
                || stateId == IsIn && piston.Status == PistonStatus.Retracted
                || stateId == There && AtLimit(piston))
            {
                return Trouble.None;
            }

            if (stateId != IsOut && stateId != IsIn && stateId != There)
                return Trouble.None;

            if (!piston.IsWorking)
                return Trouble.Waiting(Cause.PistonNoPower);

            if (!piston.IsAttached)
                return Trouble.Waiting(Cause.PistonNoHead);

            if (piston.Velocity == 0f)
                return Trouble.Fatal(Cause.PistonStopped);

            if (stateId == IsOut && piston.Velocity < 0f)
                return Trouble.Fatal(Cause.PistonReversedExtending);

            if (stateId == IsIn && piston.Velocity > 0f)
                return Trouble.Fatal(Cause.PistonReversedRetracting);

            return Trouble.None;
        }

        /// <inheritdoc />
        public Status StatusOf(IMyTerminalBlock block)
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
        public bool IsBusy(IMyTerminalBlock block)
        {
            var piston = block as IMyPistonBase;
            return piston != null
                && (piston.Status == PistonStatus.Extending || piston.Status == PistonStatus.Retracting);
        }

        /// <summary>The ticks the move should take: the distance it has to go, at the speed it goes.</summary>
        /// <remarks>Its speed is in metres a second, and the game runs sixty ticks to the second.</remarks>
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var piston = block as IMyPistonBase;
            if (piston == null)
                return GridEntity.Unestimated;

            float distance;
            float speed;
            if (actionId == MoveTo)
            {
                float position;
                if (!Given(values, out position, out speed))
                    return GridEntity.Unestimated;

                distance = Math.Abs(position - piston.CurrentPosition);
            }
            else if (actionId == Extend)
            {
                distance = piston.MaxLimit - piston.CurrentPosition;
                speed = Math.Abs(piston.Velocity);
            }
            else if (actionId == Retract)
            {
                distance = piston.CurrentPosition - piston.MinLimit;
                speed = Math.Abs(piston.Velocity);
            }
            else
            {
                return GridEntity.Unestimated;
            }

            return speed <= 0f ? GridEntity.Unestimated : (int)Math.Ceiling(Math.Max(0f, distance) / speed * 60f);
        }
    }
}
