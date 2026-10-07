using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Values;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// The air vent, which the terminal describes only as a switch and which the airlock is built around.
    /// </summary>
    /// <remarks>
    /// It has no events of any kind - see docs/Catalogue.md - so all of this is sampled, and a room that
    /// empties and refills between two ticks is invisible. That bound was accepted knowingly; it is worth
    /// remembering that the vent is the block it bites.
    /// </remarks>
    public sealed class AirVentExtension : IBlockExtension
    {
        const float Emptied = 0.05f;

        const float Filled = 0.95f;

        /// <summary>The reading of a vent whose room is full and no longer filling.</summary>
        public const string Air = "air";

        /// <summary>The reading of a vent whose room is empty and no longer emptying.</summary>
        public const string Empty = "empty";

        /// <summary>The reading of how much air the room holds.</summary>
        public const string Level = "level";

        /// <summary>The reading of a vent whose room is airtight.</summary>
        public const string Sealed = "sealed";

        /// <summary>The action that fills the room, which may be told to carry on where there is no more oxygen to give.</summary>
        public const string Pressurize = "pressurize";

        /// <summary>The action that empties the room, which may be told to carry on where the air has nowhere to go.</summary>
        public const string Depressurize = "depressurize";

        /// <summary>What a stuck room told to fill or empty does: wait until it gets there.</summary>
        public const long Hold = 0;

        /// <summary>What a stuck room told to fill or empty does: carry on once the gas has nowhere to come from or go to.</summary>
        public const long MoveOn = 1;

        readonly VentGas _gas = new VentGas();

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Air, "has air")
                .WithProperty(Empty, "is empty")
                .WithProperty(Level, ValueKind.Real, "oxygen level")
                .WithProperty(Sealed, "airtight")
                .WithEvent("filled", Air, Edge.Rising, "finishes filling").Reading("has air", "has no air")
                .Looked("A vent raises no event of its own, and nothing it announces rules a room's air in or out.")
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying").Reading("is empty", "is not empty")
                .Looked("A vent raises no event of its own, and nothing it announces rules a room's air in or out.")
                .WithState("has-air", Air, true, "has air")
                .WithState("is-empty", Empty, true, "is empty")
                .WithState("is-sealed", Sealed, true, "is airtight")
                .WithState("is-leaking", Sealed, false, "is not airtight")
                .WithAction(Pressurize, "pressurize", "has-air", "Depressurize_Off").Lasting().Asking(Stuck(
                    "**Hold** waits until the room is full. Where there is no more oxygen to give, as when the tanks are "
                    + "empty, that wait never ends.\n\n**Move on** carries on once the room will fill no further, with what "
                    + "air it has. A room that is not airtight always holds: carrying on would open it to the leak."))
                .WithAction(Depressurize, "depressurize", "is-empty", "Depressurize_On").Lasting().Asking(Stuck(
                    "**Hold** waits until the room is empty. Where its air has nowhere to go, as when the tanks are full, "
                    + "that wait never ends.\n\n**Move on** carries on once the room will empty no further, and what comes "
                    + "next, such as an outer door opening, lets the rest of the air out."));
        }

        /// <summary>
        /// What a room told to fill or empty does when it is stuck: hold, waiting until it gets there, or move on, carrying on
        /// once the gas has nowhere to come from or go to. <paramref name="about" /> says which is which for the action.
        /// </summary>
        /// <remarks>
        /// Full tanks are the common case, and an airlock waiting on them never lets anybody out. Holding is what the action
        /// does anyway, so a sentence leaves it out, and says "or move on" where it moves on.
        /// </remarks>
        static Field Stuck(string about)
        {
            var field = Field.Among("when stuck", new[] { "move on", "hold" }, new[] { Literal.Of(MoveOn).Said, Literal.Of(Hold).Said });
            field.Entry = Entry.Whole;
            field.Default = "hold";
            field.Quiet = "hold";
            field.Lead = "or";
            field.About = about;
            return field;
        }

        /// <inheritdoc />
        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var vent = block as IMyAirVent;
            if (vent == null)
                return false;

            switch (propertyId)
            {
                // <b>Neither of these reads the reported status, and that is deliberate.</b> The
                // game cannot answer either question with it: an airtight room never reports
                // Depressurized, because that value is reserved for a room that is not airtight or
                // is not there, and it only reports Pressurized once the level has reached exactly
                // one. Both are raised with the game as a bug; neither is waitable meanwhile.
                //
                // So both come from the oxygen in the room, and the vent has to be working for the
                // reading to mean anything. That last part is what keeps a loading world safe: a
                // vent with no power, or with no room worked out yet, reports nothing left whatever
                // the room actually holds. Anything Sprocket cannot read is "not yet" and never
                // "done" - a sequence held at a wait can be recovered from, and one that opened a
                // door on a room it could not read cannot.
                // Inside the band and no longer moving gas. Both halves are needed: the level
                // alone finishes a room the vent is still taking to full, and the gas alone would
                // finish one it gave up on at a tenth.
                case Air:
                    value = EntityValue.Of(vent.IsWorking && vent.GetOxygenLevel() >= Filled
                        && !_gas.Working(vent, false));
                    return true;
                case Empty:
                    // Emptied is a band rather than a reading. A sealed room with gas to spare settles below full, 95.4% on
                    // an airlock measured in game, and nothing says where a given room stops. The game's own status cannot
                    // stand in: it calls a room pressurized only at a level of one or more, and reports an empty airtight
                    // room as depressurizing.
                    value = EntityValue.Of(vent.IsWorking && vent.GetOxygenLevel() <= Emptied
                        && !_gas.Working(vent, true));
                    return true;
                case Level:
                    value = EntityValue.Of(vent.GetOxygenLevel());
                    return true;
                case Sealed:
                    value = EntityValue.Of(vent.CanPressurize);
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return false;

            if (actionId == Pressurize)
            {
                vent.Depressurize = false;
                return true;
            }

            if (actionId == Depressurize)
            {
                vent.Depressurize = true;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public Status StatusOf(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return Status.None;

            // What it cannot do comes before what it says it is doing, asked about the thing it is
            // currently set to do. A vent that has stopped moving gas short of where it was going
            // goes on reporting Pressurizing for as long as the world exists, so the game's own
            // word for it is the one thing that never says so.
            Trouble stuck = Said(vent);
            if (stuck.Exists)
                return Status.Of(stuck);

            switch (vent.Status)
            {
                case VentStatus.Pressurized: return Status.Of(Condition.Pressurized);
                case VentStatus.Depressurized: return Status.Of(Condition.Empty);
                case VentStatus.Pressurizing: return Status.Of(Condition.Filling);
                case VentStatus.Depressurizing: return Status.Of(Condition.Emptying);
            }

            return Status.None;
        }

        /// <summary>
        /// Why this vent will not finish what it was told to do, or null while it might.
        /// </summary>
        /// <remarks>
        /// Stopped short: it is not moving gas and the room is nowhere near where it was sent. The
        /// two directions fail for different reasons and a person can act on either one, so they
        /// are said apart rather than as one word.
        /// </remarks>
        public Trouble TroubleWith(IMyTerminalBlock block, string actionId, IList<Literal> values, string stateId)
        {
            var vent = block as IMyAirVent;
            if (vent == null || !vent.IsWorking)
                return Trouble.None;

            // What a step told to move on lets pass is no trouble to it: it is done.
            if (stateId == "is-empty")
            {
                var emptying = Emptying(vent);
                return Passes(emptying, actionId, values) ? Trouble.None : emptying;
            }

            if (stateId == "has-air")
            {
                var filling = Filling(vent);
                return Passes(filling, actionId, values) ? Trouble.None : filling;
            }

            return Trouble.None;
        }

        /// <summary>
        /// Whether a room told to fill or empty, and to move on, has done what it was told short of the state: once all that
        /// keeps it from full is that there is no more oxygen to give, or from empty that the air has nowhere to go.
        /// </summary>
        public bool Done(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var vent = block as IMyAirVent;
            if (vent == null || !vent.IsWorking)
                return false;

            if (actionId == Depressurize)
                return Passes(Emptying(vent), actionId, values);

            return actionId == Pressurize && Passes(Filling(vent), actionId, values);
        }

        /// <summary>
        /// Whether <paramref name="trouble" /> is one a step told <paramref name="values" /> lets pass: nothing to move the gas
        /// with, where it was told to move on.
        /// </summary>
        /// <remarks>A room that is not airtight never passes: carrying on would open it to the leak.</remarks>
        static bool Passes(Trouble trouble, string actionId, IList<Literal> values)
        {
            if (values == null || values.Count == 0 || values[0].Kind != ValueKind.Integer || values[0].Whole != MoveOn)
                return false;

            return actionId == Depressurize ? trouble.Cause == Cause.VentNowhere
                : actionId == Pressurize && trouble.Cause == Cause.VentNoOxygen;
        }

        /// <summary>
        /// Why the room will not finish what the vent is set to do, for a screen rather than for a step.
        /// </summary>
        /// <remarks>
        /// Reads the gas without keeping a clock on it. Drawing a status must not move the timing
        /// the runtime settles a step by, and it has no use for it: what is on screen is about this
        /// moment and is drawn again a moment later.
        /// </remarks>
        Trouble Said(IMyAirVent vent)
        {
            if (vent.Depressurize)
                return vent.GetOxygenLevel() > Emptied && !_gas.Moving(vent, true)
                    ? Trouble.Waiting(Cause.VentNowhere)
                    : Trouble.None;

            if (vent.GetOxygenLevel() >= Filled)
                return Trouble.None;

            if (!vent.CanPressurize)
                return Trouble.Waiting(Cause.VentNotAirtight);

            return _gas.Moving(vent, false)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNoOxygen);
        }

        /// <summary>
        /// Why the room will not empty, or null while it still might.
        /// </summary>
        /// <remarks>
        /// Told to fill instead is its own answer and not a silence. Two sequences share one vent -
        /// a cycle out and a cycle in - and the one that has been overruled is left waiting for a
        /// state nothing is heading towards any more.
        /// </remarks>
        Trouble Emptying(IMyAirVent vent)
        {
            if (vent.GetOxygenLevel() <= Emptied)
                return Trouble.None;

            if (!vent.Depressurize)
                return Trouble.Fatal(Cause.VentToldToFill);

            return _gas.Working(vent, true)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNowhere);
        }

        /// <summary>Why the room will not fill, or null while it still might.</summary>
        Trouble Filling(IMyAirVent vent)
        {
            if (vent.GetOxygenLevel() >= Filled)
                return Trouble.None;

            if (vent.Depressurize)
                return Trouble.Fatal(Cause.VentToldToEmpty);

            if (!vent.CanPressurize)
                return Trouble.Waiting(Cause.VentNotAirtight);

            return _gas.Working(vent, false)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNoOxygen);
        }

        /// <inheritdoc />
        public bool IsBusy(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            return vent != null
                && (vent.Status == VentStatus.Pressurizing || vent.Status == VentStatus.Depressurizing);
        }

        /// <summary>Nothing: a vent goes on with the air it was moving, and a room half emptied is no kind of stop.</summary>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
        }

        /// <inheritdoc />
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return GridEntity.Unestimated;
        }
    }
}
