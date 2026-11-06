using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A door, or a block shaped like one: sliding, airtight, hangar and the advanced door.</summary>
    public sealed partial class Door : Capability
    {
        // The delay slider the Door Auto-Close mod adds to every door, there only where that mod is loaded.
        const string AutoClose = "Sprocket_AutoClose";

        static readonly Reading Opened = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Open);

        static readonly Reading Shut = Reading.Of<IMyDoor>(door => door.Status == DoorStatus.Closed);

        static readonly string[] Statuses = { "open", "closed", "opening", "closing" };

        static readonly OwnValue State = OwnValue.OneOf<IMyDoor>(Statuses, door => Position(door.Status));

        // The game's ratio is nought to one; the terminal and a sentence say a percentage.
        static readonly OwnValue Ratio = OwnValue.Number<IMyDoor>(door => door.OpenRatio * 100.0);

        static readonly OwnAction Opens = OwnAction.Of<IMyDoor, DoorOrder>(DoorOrder.Read, (door, order, seen) => door.OpenDoor(), Opened,
            trouble: (door, order, seen) => Closing(door), forced: order => order.IsForced);

        static readonly OwnAction Closes = OwnAction.Of<IMyDoor, DoorOrder>(DoorOrder.Read, (door, order, seen) => door.CloseDoor(), Shut,
            trouble: (door, order, seen) => Opening(door), forced: order => order.IsForced);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDoor;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // Every door says when it is told to open; only a hangar door says when it is told to close, so any other door is
            // looked at between starting to open and having closed.
            var hangar = block is IMyAirtightHangarDoor;
            kind.WithValue("status", "status", State).Status();
            Watched(kind, hangar);

            kind.WithChange("finishes_opening", "finishes opening", "status", Literal.Name("open")).Announced()
                .Reading("is open", "is not open")
                .WithChange("finishes_closing", "finishes closing", "status", Literal.Name("closed")).Announced()
                .Reading("is closed", "is not closed")
                .WithChange("starts_opening", "starts opening", "status", Literal.Name("opening")).Announced()
                .Reading("is opening", "is not opening")
                .WithChange("starts_closing", "starts closing", "status", Literal.Name("closing"))
                .Reading("is closing", "is not closing");

            Watched(kind, hangar);

            // How far open changes only while the door moves, which raises nothing, so it is looked at on the when's cadence
            // between starting to open and having closed.
            kind.WithValue("how_far_open", "how far open", Ratio).Percent().RuledOutBy(Shut);

            kind.WithAction("open", "open", Opens, "Open_On").LastingAnnounced().Asking(DoorOrder.Asked())
                .WithAction("close", "close", Closes, "Open_Off").LastingAnnounced().Asking(DoorOrder.Asked())
                .Renaming("Open", false, "reverse")
                .Renaming("AnyoneCanUse", true, "anyone can use")
                .Renaming("AnyoneCanUse", false, "toggle anyone can use")
                .Renaming(AutoClose, true, "auto-close delay")
                .Measuring(AutoClose, " s");
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
        static BlockTrouble Opening(IMyDoor door)
        {
            // A door's status follows the last order it was given.
            return door.Status == DoorStatus.Opening || door.Status == DoorStatus.Open
                ? BlockTrouble.Fatal(Cause.DoorToldToOpen)
                : BlockTrouble.None;
        }

        /// <summary>Why a door told to open is not getting there: something else has told it to close.</summary>
        static BlockTrouble Closing(IMyDoor door)
        {
            return door.Status == DoorStatus.Closing || door.Status == DoorStatus.Closed
                ? BlockTrouble.Fatal(Cause.DoorToldToClose)
                : BlockTrouble.None;
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

        /// <summary>Told when the door finishes opening or closing, as it is told to open, and for a hangar door as it is told to close.</summary>
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
    }
}
