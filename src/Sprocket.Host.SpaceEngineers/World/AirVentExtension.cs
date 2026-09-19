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
        /// <summary>
        /// The oxygen left at which a room counts as empty, and the amount at which it counts as full.
        /// </summary>
        /// <remarks>
        /// A tolerance at both ends, because a room's oxygen is a float that is pumped towards a
        /// figure rather than set to it: it may never read exactly zero even in vacuum, and never
        /// exactly one when it is as full as it is going to get.
        /// <para>
        /// Empty is the game's own number for the same judgement - it calls an unsealed room empty
        /// below this. Full is its mirror, because the game has no tolerance there at all and
        /// compares against one exactly.
        /// </para>
        /// </remarks>
        const float Emptied = 0.01f;

        const float Filled = 0.99f;

        public const string Air = "air";
        public const string Empty = "empty";
        public const string Level = "level";
        public const string Sealed = "sealed";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Air, "has air")
                .WithProperty(Empty, "is empty")
                .WithProperty(Level, ValueKind.Real, "oxygen level")
                .WithProperty(Sealed, "airtight")
                .WithEvent("filled", Air, Edge.Rising, "finishes filling")
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying")
                .WithState("has-air", Air, true, "has air")
                .WithState("is-empty", Empty, true, "is empty")
                .WithState("is-sealed", Sealed, true, "is airtight")
                .WithState("is-leaking", Sealed, false, "is not airtight")
                .WithAction("pressurize", "pressurize", "has-air", "Depressurize_Off")
                .WithAction("depressurize", "depressurize", "is-empty", "Depressurize_On");
        }

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
                case Air:
                    value = EntityValue.Of(vent.IsWorking && vent.GetOxygenLevel() >= Filled);
                    return true;
                case Empty:
                    value = EntityValue.Of(vent.IsWorking && vent.GetOxygenLevel() <= Emptied);
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

        public bool TryInvoke(IMyTerminalBlock block, string actionId)
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

        public string StatusOf(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return null;

            switch (vent.Status)
            {
                case VentStatus.Pressurized: return "pressurized";
                case VentStatus.Depressurized: return "empty";
                case VentStatus.Pressurizing: return "filling…";
                case VentStatus.Depressurizing: return "emptying…";
            }

            return null;
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            return vent != null
                && (vent.Status == VentStatus.Pressurizing || vent.Status == VentStatus.Depressurizing);
        }
    }
}
