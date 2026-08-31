using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.Terminal;

namespace Reactor.Host.SpaceEngineers.World
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
        public const string Air = "air";
        public const string Empty = "empty";
        public const string Level = "level";
        public const string Sealed_ = "sealed";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        public void Describe(EntityKind kind)
        {
            kind.WithProperty(Air, "has air")
                .WithProperty(Empty, "is empty")
                .WithProperty(Level, ValueKind.Real, "oxygen level")
                .WithProperty(Sealed_, "airtight")
                .WithEvent("filled", Air, Edge.Rising, "finishes filling")
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying")
                .WithState("has-air", Air, true, "has air")
                .WithState("is-empty", Empty, true, "is empty")
                .WithState("is-sealed", Sealed_, true, "is airtight")
                .WithState("is-leaking", Sealed_, false, "is not airtight")
                .WithAction("pressurize", "pressurize", "has-air")
                .WithAction("depressurize", "depressurize", "is-empty");
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var vent = block as IMyAirVent;
            if (vent == null)
                return false;

            switch (propertyId)
            {
                case Air:
                    value = EntityValue.Of(vent.Status == VentStatus.Pressurized);
                    return true;
                case Empty:
                    value = EntityValue.Of(vent.Status == VentStatus.Depressurized);
                    return true;
                case Level:
                    value = EntityValue.Of(vent.GetOxygenLevel());
                    return true;
                case Sealed_:
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

        public IBlockListener Listen(IMyTerminalBlock block)
        {
            return null;
        }
    }
}
