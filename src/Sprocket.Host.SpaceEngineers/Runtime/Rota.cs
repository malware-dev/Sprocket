using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// The world's timetable: everything the runtime looks at, and the tick each thing is looked at on.
    /// </summary>
    /// <remarks>
    /// One for the whole world, and a tick visits only what is due on it. The three periods are the three the game wakes
    /// a mod at. Every tick number from the last one run is walked, so a frame counter that jumps leaves nothing behind.
    /// </remarks>
    public sealed class Rota
    {
        /// <summary>Looked at on every tick.</summary>
        public const int Each = 1;

        /// <summary>Looked at once in ten ticks.</summary>
        public const int Ten = 10;

        /// <summary>Looked at once in a hundred ticks, which is also how many places there are.</summary>
        public const int Hundred = 100;

        readonly List<Shift> _each = new List<Shift>();
        readonly List<Shift>[] _tens = Lists(Ten);
        readonly List<Shift>[] _hundreds = Lists(Hundred);

        readonly List<Shift> _due = new List<Shift>();

        int _next;
        int _last;
        bool _started;
        int _count;

        /// <summary>A place in the turn for something new.</summary>
        public int Join()
        {
            var place = _next;
            _next = (_next + 1) % Hundred;
            return place;
        }

        /// <summary>How many shifts have been done, all told.</summary>
        public int Done { get; private set; }

        /// <summary>How many shifts are on the rota.</summary>
        /// <remarks>Kept as shifts join and leave, since the session asks it on every tick.</remarks>
        public int Count => _count;

        /// <summary>Looks at a shift once every period, on its place, until told otherwise.</summary>
        public void Every(Shift shift, int period)
        {
            var list = period <= Each ? _each
                : period <= Ten ? _tens[shift.Place % Ten]
                : _hundreds[shift.Place % Hundred];

            if (shift.List == list && !shift.Once)
                return;

            Leave(shift);
            shift.Once = false;
            Put(shift, list);

            var kept = shift.Kept;
            if (kept == null)
                return;

            kept.Period = period <= Each ? Each : period <= Ten ? Ten : Hundred;
            kept.Once = false;
        }

        /// <summary>Looks at a shift once, on the given tick, or on the next one run if that has passed.</summary>
        public void At(Shift shift, int tick)
        {
            Leave(shift);
            shift.Once = true;
            shift.At = tick;

            // A deadline sits in the list for its own tick's place, so it is visited once a hundred
            // ticks at most and done on the tick itself.
            var soon = !_started || tick <= _last + 1;
            Put(shift, soon ? _each : _hundreds[Place(tick, Hundred)]);

            var kept = shift.Kept;
            if (kept == null)
                return;

            kept.Period = 0;
            kept.Once = true;
            kept.At = tick;
        }

        /// <summary>Puts a shift back where what it keeps says it stood when the world was saved: on its turn, or due once on its tick.</summary>
        public void Restore(Shift shift)
        {
            var kept = shift.Kept;
            if (kept == null)
                return;

            if (kept.Once)
                At(shift, kept.At);
            else if (kept.Period > 0)
                Every(shift, kept.Period);
        }

        /// <summary>Takes a shift off the rota.</summary>
        public void Leave(Shift shift)
        {
            var list = shift.List;
            if (list == null)
                return;

            var kept = shift.Kept;
            if (kept != null)
            {
                kept.Period = 0;
                kept.Once = false;
            }

            var last = list.Count - 1;
            var moved = list[last];
            list[shift.Index] = moved;
            moved.Index = shift.Index;
            list.RemoveAt(last);

            shift.List = null;
            _count--;
        }

        /// <summary>Does everything due from the tick after the last one run up to this one.</summary>
        public void Run(int tick)
        {
            // A world begun, a counter that went backwards, or a gap longer than a whole turn: a
            // hundred ticks visit every list once, which is all a gap of any length can owe.
            if (!_started || tick <= _last)
                _last = tick - 1;
            else if (tick - _last > Hundred)
                _last = tick - Hundred;

            _started = true;

            while (_last < tick)
            {
                _last++;
                Turn(_last);
            }
        }

        /// <summary>Does what is due on one tick.</summary>
        void Turn(int tick)
        {
            _due.Clear();
            _due.AddRange(_each);
            _due.AddRange(_tens[Place(tick, Ten)]);
            _due.AddRange(_hundreds[Place(tick, Hundred)]);

            foreach (var shift in _due)
            {
                if (shift.List == null || shift.Ran == tick)
                    continue;

                if (shift.Once)
                {
                    if (shift.At > tick)
                        continue;

                    Leave(shift);
                }

                shift.Ran = tick;
                Done++;
                shift.Due(tick);
            }

            _due.Clear();
        }

        void Put(Shift shift, List<Shift> list)
        {
            shift.List = list;
            shift.Index = list.Count;
            list.Add(shift);
            _count++;
        }

        static int Place(int tick, int period)
        {
            var at = tick % period;
            return at < 0 ? at + period : at;
        }

        static List<Shift>[] Lists(int count)
        {
            var lists = new List<Shift>[count];
            for (var i = 0; i < count; i++)
                lists[i] = new List<Shift>();
            return lists;
        }
    }
}
