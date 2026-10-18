using System;
using Sandbox.ModAPI;

// IMyDoor is declared in both the ModAPI and the Ingame namespace, so the Ingame one cannot be imported whole.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Closes one door the delay it keeps after it finishes opening.</summary>
    public sealed class DoorCloser : IDisposable
    {
        readonly IMyDoor _door;
        readonly Func<int> _now;
        readonly Action _changed;

        int _due = -1;

        /// <summary>
        /// A closer for <paramref name="door" />, counting in the ticks <paramref name="now" /> answers, and telling
        /// <paramref name="changed" /> each time a close starts or stops coming.
        /// </summary>
        public DoorCloser(IMyDoor door, Func<int> now, Action changed)
        {
            _door = door;
            _now = now;
            _changed = changed;

            _door.OnDoorOpened += Opened;
            _door.DoorStateChanged += Moved;
        }

        /// <summary>Whether a close is coming.</summary>
        public bool Waiting => _due >= 0;

        /// <summary>The tick a close is coming on, or -1 where none is.</summary>
        public int ClosesAt => _due;

        /// <summary>
        /// Starts closing a door that may already stand open: on the tick <paramref name="due" /> names, the close a saved world
        /// was waiting for, or by its delay counted from now where that is -1.
        /// </summary>
        public void Start(int due)
        {
            if (_door.Status != DoorStatus.Open)
                return;

            if (due >= 0)
                Wait(due);
            else
                Opened(_door);
        }

        /// <summary>Takes up a delay of <paramref name="seconds" /> just set, counting from now where the door stands open; none stops a coming close.</summary>
        public void Set(int seconds)
        {
            Wait(-1);
            if (seconds > 0 && _door.Status == DoorStatus.Open)
                Wait(_now() + seconds * CloseDelay.TicksPerSecond);
        }

        /// <summary>Closes the door where its close has come.</summary>
        /// <remarks>Asked every tenth frame while a close is coming; costs a comparison.</remarks>
        public void Check()
        {
            if (_due < 0 || _now() < _due)
                return;

            Wait(-1);
            if (!_door.Closed && _door.Status == DoorStatus.Open)
                _door.CloseDoor();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _door.OnDoorOpened -= Opened;
            _door.DoorStateChanged -= Moved;
            Wait(-1);
        }

        void Opened(IMyDoor door)
        {
            // A damaged plain door raises OnDoorOpened on every tick it stands open.
            if (_due >= 0)
                return;

            var seconds = CloseDelay.Stored(_door);
            if (seconds > 0)
                Wait(_now() + seconds * CloseDelay.TicksPerSecond);
        }

        void Moved(bool open)
        {
            Wait(-1);
        }

        void Wait(int due)
        {
            var was = Waiting;
            _due = due;
            if (Waiting != was && _changed != null)
                _changed();
        }
    }
}
