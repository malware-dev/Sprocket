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

        static readonly string[] Statuses = { "open", "closed", "opening", "closing" };

        static readonly Being State = Being.OneOf<IMyDoor>(Statuses, door => Position(door.Status));

        // The game's ratio is nought to one; the terminal and a sentence say a percentage.
        static readonly Being Ratio = Being.Number<IMyDoor>(door => door.OpenRatio * 100.0);

        static readonly Doing Opens = Doing.Of<IMyDoor>(door => door.OpenDoor(), Opened, Closing);

        static readonly Doing Closes = Doing.Of<IMyDoor>(door => door.CloseDoor(), Shut, Opening);

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
            // Every door says when it is told to open; only a hangar door says when it is told to close. Any other door is
            // looked at for starting to close only between starting to open and having closed, which it does say, and so
            // is its status as a whole.
            var hangar = block is IMyAirtightHangarDoor;
            kind.WithValue("status", "status", State).Status();
            Watched(kind, hangar);

            kind.WithChange("finishes-opening", "finishes opening", "status", Literal.Name("open")).Announced()
                .Reading("is open", "is not open")
                .WithChange("finishes-closing", "finishes closing", "status", Literal.Name("closed")).Announced()
                .Reading("is closed", "is not closed")
                .WithChange("starts-opening", "starts opening", "status", Literal.Name("opening")).Announced()
                .Reading("is opening", "is not opening")
                .WithChange("starts-closing", "starts closing", "status", Literal.Name("closing"))
                .Reading("is closing", "is not closing");

            Watched(kind, hangar);

            // How far open changes only while the door moves, which it can only between starting to open and having
            // closed; nothing is raised as it moves, so it is looked at on the when's cadence in between.
            kind.WithValue("how-far-open", "how far open", Ratio).RuledOutBy(Shut);

            kind.WithAction("open", "open", Opens, "Open_On").Lasting()
                .WithAction("close", "close", Closes, "Open_Off").Lasting()
                .Renaming("Open", false, "reverse")
                .Renaming("AnyoneCanUse", true, "anyone can use")
                .Renaming("AnyoneCanUse", false, "toggle anyone can use");
        }

        /// <summary>Says how what was added last is heard: announced on a hangar door, and on any other ruled out while it is shut.</summary>
        static void Watched(EntityKind kind, bool hangar)
        {
            if (hangar)
                kind.Announced();
            else
                kind.RuledOutBy(Shut);
        }

        /// <summary>Why a door told to close is not getting there: something else has told it to open.</summary>
        /// <remarks>A door's status follows the last order it was given, so opening or open means it was told to.</remarks>
        static Trouble Opening(IMyDoor door)
        {
            return door.Status == DoorStatus.Opening || door.Status == DoorStatus.Open
                ? Trouble.Fatal(Cause.DoorToldToOpen)
                : Trouble.None;
        }

        /// <summary>Why a door told to open is not getting there: something else has told it to close.</summary>
        static Trouble Closing(IMyDoor door)
        {
            return door.Status == DoorStatus.Closing || door.Status == DoorStatus.Closed
                ? Trouble.Fatal(Cause.DoorToldToClose)
                : Trouble.None;
        }

        /// <summary>Where <paramref name="status" /> stands among the door's statuses.</summary>
        static int Position(DoorStatus status)
        {
            switch (status)
            {
                case DoorStatus.Open:
                    return 0;
                case DoorStatus.Closed:
                    return 1;
                case DoorStatus.Opening:
                    return 2;
            }

            return 3;
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
