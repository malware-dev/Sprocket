using System.Collections.Generic;
using Sandbox.ModAPI;
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
    sealed class AirVentExtension : IBlockExtension
    {
        const float Emptied = 0.05f;

        const float Filled = 0.95f;

        public const string Air = "air";
        public const string Empty = "empty";
        public const string Level = "level";
        public const string Sealed = "sealed";

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
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying").Reading("is empty", "is not empty")
                .WithState("has-air", Air, true, "has air")
                .WithState("is-empty", Empty, true, "is empty")
                .WithState("is-sealed", Sealed, true, "is airtight")
                .WithState("is-leaking", Sealed, false, "is not airtight")
                .WithAction("pressurize", "pressurize", "has-air", "Depressurize_Off").Lasting()
                .WithAction("depressurize", "depressurize", "is-empty", "Depressurize_On").Lasting();
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

            if (actionId == "pressurize")
            {
                vent.Depressurize = false;
                return true;
            }

            if (actionId == "depressurize")
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
        public Trouble TroubleWith(IMyTerminalBlock block, string stateId)
        {
            var vent = block as IMyAirVent;
            if (vent == null || !vent.IsWorking)
                return Trouble.None;

            if (stateId == "is-empty")
                return Emptying(vent);

            return stateId == "has-air" ? Filling(vent) : Trouble.None;
        }

        /// <summary>
        /// Why the room will not empty, or null while it still might.
        /// </summary>
        /// <remarks>
        /// Told to fill instead is its own answer and not a silence. Two sequences share one vent -
        /// a cycle out and a cycle in - and the one that has been overruled is left waiting for a
        /// state nothing is heading towards any more.
        /// </remarks>
        /// <summary>
        /// The same, for a screen rather than for a step.
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
