using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;

// IMyDoor is declared in both the ModAPI and the Ingame namespace, so the Ingame one cannot be imported whole.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Closes one door a set time after it finishes opening.</summary>
    public sealed class DoorCloser : IDisposable
    {
        readonly IMyDoor _door;
        readonly Rota _rota;
        readonly Func<int> _now;
        readonly Shift _shift;

        int _ticks;

        /// <summary>A closer for <paramref name="door" />, counting on <paramref name="rota" /> from the tick <paramref name="now" /> answers.</summary>
        public DoorCloser(IMyDoor door, Rota rota, Func<int> now)
        {
            _door = door;
            _rota = rota;
            _now = now;
            _shift = new Shift(rota.Join(), Due);

            _door.OnDoorOpened += Opened;
            _door.DoorStateChanged += Moved;
        }

        /// <summary>The door this closes.</summary>
        public IMyDoor Door => _door;

        /// <summary>Whether a close is coming.</summary>
        public bool Waiting => _shift.Listed;

        /// <summary>The tick a close is coming on, or -1 where none is.</summary>
        public int ClosesAt => _shift.Listed ? _shift.At : -1;

        /// <summary>Closes the door <paramref name="seconds" /> after it finishes opening, counting from now where it is open already.</summary>
        public void Set(int seconds)
        {
            Set(seconds, -1);
        }

        /// <summary>
        /// Closes the door <paramref name="seconds" /> after it finishes opening; where it is open already, on the tick
        /// <paramref name="due" /> names, the close a saved world was waiting for, or counting from now where that is -1.
        /// </summary>
        public void Set(int seconds, int due)
        {
            _ticks = seconds * CloseDelay.TicksPerSecond;
            _rota.Leave(_shift);
            if (_door.Status != DoorStatus.Open)
                return;

            if (due >= 0)
                _rota.At(_shift, due);
            else
                Opened(_door);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _door.OnDoorOpened -= Opened;
            _door.DoorStateChanged -= Moved;
            _rota.Leave(_shift);
        }

        void Opened(IMyDoor door)
        {
            // A damaged plain door raises OnDoorOpened on every tick it stands open.
            if (!_shift.Listed)
                _rota.At(_shift, _now() + _ticks);
        }

        void Moved(bool open)
        {
            _rota.Leave(_shift);
        }

        void Due(int tick)
        {
            if (!_door.Closed && _door.Status == DoorStatus.Open)
                _door.CloseDoor();
        }
    }
}
