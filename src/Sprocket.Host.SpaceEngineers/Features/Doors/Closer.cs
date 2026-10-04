using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor is declared in both, and the
// mod-side one is the one with the events on it.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Closes one door a set time after it finishes opening.</summary>
    /// <remarks>
    /// The count starts when the door is fully open, which every family of door announces with <c>OnDoorOpened</c>. Every
    /// door also raises <c>DoorStateChanged</c> as it is told to open and as it finishes closing, and either one ends the
    /// count: a door closed by hand is left alone, and a door opened again counts afresh once it is open.
    /// <para>
    /// A plain door that is damaged raises <c>OnDoorOpened</c> on every tick it stands open, so an opening that arrives
    /// while the count is running leaves it as it is.
    /// </para>
    /// </remarks>
    public sealed class Closer : IDisposable
    {
        readonly IMyDoor _door;
        readonly Rota _rota;
        readonly Func<int> _now;
        readonly Shift _shift;

        int _ticks;

        /// <summary>A closer for <paramref name="door" />, counting on <paramref name="rota" /> from the tick <paramref name="now" /> answers.</summary>
        public Closer(IMyDoor door, Rota rota, Func<int> now)
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

        /// <summary>Closes the door <paramref name="seconds" /> after it finishes opening, counting from now where it is open already.</summary>
        public void Set(int seconds)
        {
            _ticks = seconds * CloseDelay.TicksPerSecond;
            _rota.Leave(_shift);
            if (_door.Status == DoorStatus.Open)
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
