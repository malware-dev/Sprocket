using Sandbox.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// Doors, and everything shaped like one - sliding, airtight, hangar, and the advanced door.
    /// </summary>
    /// <remarks>
    /// The terminal has one <c>Open</c> control, which is the switch a player throws rather than where the
    /// door has got to. Reactor needs the difference: "close the inner door" is finished when the door is
    /// shut, not when the order was given.
    /// </remarks>
    sealed class DoorExtension : IBlockExtension
    {
        public const string Open = "open";
        public const string Shut = "shut";
        public const string Moving = "moving";

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDoor;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Open, "open")
                .WithProperty(Shut, "shut")
                .WithProperty(Moving, "moving")
                .WithEvent("opened", Open, Edge.Rising, "finishes opening")
                .WithEvent("closed", Shut, Edge.Rising, "finishes closing")
                .WithState("is-open", Open, true, "is open")
                .WithState("is-shut", Shut, true, "is shut")
                .WithState("is-moving", Moving, true, "is moving")
                .WithAction("open", "open", "is-open")
                .WithAction("close", "close", "is-shut");
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var door = block as IMyDoor;
            if (door == null)
                return false;

            switch (propertyId)
            {
                case Open:
                    value = EntityValue.Of(door.Status == DoorStatus.Open);
                    return true;
                case Shut:
                    value = EntityValue.Of(door.Status == DoorStatus.Closed);
                    return true;
                case Moving:
                    value = EntityValue.Of(door.Status == DoorStatus.Opening
                        || door.Status == DoorStatus.Closing);
                    return true;
            }

            return false;
        }

        public bool TryInvoke(IMyTerminalBlock block, string actionId)
        {
            var door = block as IMyDoor;
            if (door == null)
                return false;

            if (actionId == "open")
            {
                door.OpenDoor();
                return true;
            }

            if (actionId == "close")
            {
                door.CloseDoor();
                return true;
            }

            return false;
        }

        public string StatusOf(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            if (door == null)
                return null;

            switch (door.Status)
            {
                case DoorStatus.Open: return "open";
                case DoorStatus.Closed: return "shut";
                case DoorStatus.Opening: return "opening…";
                case DoorStatus.Closing: return "closing…";
            }

            return null;
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            return door != null
                && (door.Status == DoorStatus.Opening || door.Status == DoorStatus.Closing);
        }
    }
}
