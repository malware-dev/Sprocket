using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// Doors, and everything shaped like one - sliding, airtight, hangar, and the advanced door.
    /// </summary>
    /// <remarks>
    /// The terminal has one <c>Open</c> control, which is the switch a player throws rather than where the
    /// door has got to. Sprocket needs the difference: "close the inner door" is finished when the door is
    /// shut, not when the order was given.
    /// </remarks>
    public sealed class Door : Capability
    {
        static readonly Reading Opened = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Open);

        static readonly Reading Shut = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Closed);

        static readonly Reading Opening = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Opening);

        static readonly Reading Closing = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Closing);

        static readonly Doing Opens = Doing.Of<IMyDoor>(door => door.OpenDoor(), Opened);

        static readonly Doing Closes = Doing.Of<IMyDoor>(door => door.CloseDoor(), Shut);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDoor;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Nothing to call off: a door goes on to where it was going, and one stopped half way is a hole in a wall.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithEvent("opened", Opened, Edge.Rising, "finishes opening").Announced().Reading("is open", "is not open")
                .WithEvent("closed", Shut, Edge.Rising, "finishes closing").Announced().Reading("is closed", "is not closed")
                .WithEvent("opening", Opening, Edge.Rising, "starts opening").Announced().Reading("is opening", "is not opening")
                .WithEvent("closing", Closing, Edge.Rising, "starts closing").Reading("is closing", "is not closing");

            // Every door says when it is told to open; only a hangar door says when it is told to close. Any other door is
            // looked at for starting to close only between starting to open and having closed, which it does say.
            if (block is IMyAirtightHangarDoor)
                kind.Announced();
            else
                kind.RuledOutBy(Shut);

            kind.WithAction("open", "open", Opens, "Open_On").Lasting()
                .WithAction("close", "close", Closes, "Open_Off").Lasting()
                .Renaming("Open", false, "reverse");
        }

        /// <summary>Told when the door finishes opening or closing, which every door raises from its own update, as it is told to open, and, for a hangar door, as it is told to close.</summary>
        /// <remarks>
        /// Each family of door raises these itself, and a plain door may raise "closed" as a world loads with it shut; a
        /// look finds nothing changed and nothing fires. A door's state-changed event comes as it is told to open and again
        /// once it has closed, never as it is told to close; a hangar door's properties-changed event comes as it is told to
        /// do either.
        /// </remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var door = block as IMyDoor;
            return door == null ? null : new Listening(door, told);
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
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
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var door = block as IMyDoor;
            return door != null
                && (door.Status == DoorStatus.Opening || door.Status == DoorStatus.Closing);
        }

        /// <summary>A door being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyDoor _door;
            readonly Action _told;
            readonly bool _hangar;

            public Listening(IMyDoor door, Action told)
            {
                _door = door;
                _told = told;
                _hangar = door is IMyAirtightHangarDoor;
                _door.OnDoorOpened += Moved;
                _door.OnDoorClosed += Moved;
                _door.OnDoorStateChanged += Turned;
                if (_hangar)
                    _door.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _door.OnDoorOpened -= Moved;
                _door.OnDoorClosed -= Moved;
                _door.OnDoorStateChanged -= Turned;
                if (_hangar)
                    _door.PropertiesChanged -= Changed;
            }

            void Moved(IMyDoor door)
            {
                _told();
            }

            void Turned(IMyDoor door, bool open)
            {
                _told();
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
