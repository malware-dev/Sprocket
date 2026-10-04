using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Doors, and everything shaped like one - sliding, airtight, hangar, and the advanced door.
    /// </summary>
    /// <remarks>
    /// The terminal has one <c>Open</c> control, which is the switch a player throws rather than where the
    /// door has got to. Sprocket needs the difference: "close the inner door" is finished when the door is
    /// shut, not when the order was given.
    /// </remarks>
    sealed class DoorExtension : IBlockExtension, IAnnounces
    {
        public const string Open = "open";
        public const string Shut = "shut";
        public const string Moving = "moving";

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDoor;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Open, "open")
                .WithProperty(Shut, "shut")
                .WithProperty(Moving, "moving")
                .WithEvent("opened", Open, Edge.Rising, "finishes opening").Announced().Reading("is open", "is not open")
                .WithEvent("closed", Shut, Edge.Rising, "finishes closing").Announced().Reading("is closed", "is not closed")
                .WithState("is-open", Open, true, "is open")
                .WithState("is-shut", Shut, true, "is shut")
                .WithState("is-moving", Moving, true, "is moving")
                .WithAction("open", "open", "is-open", "Open_On").Lasting()
                .WithAction("close", "close", "is-shut", "Open_Off").Lasting()
                .Renaming("Open", false, "reverse");
        }

        /// <summary>Told when the door finishes opening or closing, which every door raises from its own update.</summary>
        /// <remarks>
        /// Each family of door raises these itself, and a plain door may raise "closed" as a world loads with it shut; a
        /// look finds nothing changed and nothing fires.
        /// </remarks>
        public IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var door = block as IMyDoor;
            return door == null ? null : new Listening(door, told);
        }

        /// <summary>A door being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyDoor _door;
            readonly Action _told;

            public Listening(IMyDoor door, Action told)
            {
                _door = door;
                _told = told;
                _door.OnDoorOpened += Moved;
                _door.OnDoorClosed += Moved;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _door.OnDoorOpened -= Moved;
                _door.OnDoorClosed -= Moved;
            }

            void Moved(IMyDoor door)
            {
                _told();
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
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


        /// <summary>Nothing: nothing known about this block says why it would not finish.</summary>

        /// <inheritdoc />
        public Trouble TroubleWith(IMyTerminalBlock block, string stateId)
        {
            return Trouble.None;
        }

        /// <inheritdoc />
        public Status StatusOf(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            if (door == null)
                return Status.None;

            switch (door.Status)
            {
                case DoorStatus.Open: return Status.Of(Condition.Open);
                case DoorStatus.Closed: return Status.Of(Condition.Shut);
                case DoorStatus.Opening: return Status.Of(Condition.Opening);
                case DoorStatus.Closing: return Status.Of(Condition.Closing);
            }

            return Status.None;
        }

        /// <inheritdoc />
        public bool IsBusy(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            return door != null
                && (door.Status == DoorStatus.Opening || door.Status == DoorStatus.Closing);
        }

        /// <summary>Nothing: a door goes on to where it was going, and one stopped half way is a hole in a wall.</summary>
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
