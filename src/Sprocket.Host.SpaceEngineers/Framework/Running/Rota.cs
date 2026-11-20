using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
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

        readonly List<Shift> _first = new List<Shift>();
        readonly List<Shift> _each = new List<Shift>();
        readonly List<Shift>[] _tens = Lists(Ten);
        readonly List<Shift>[] _hundreds = Lists(Hundred);

        readonly List<Shift> _due = new List<Shift>();

        int _next;
        int _last;
        int _begun;
        bool _started;
        int _count;

        /// <summary>The tick being run, or the last one run; before the first, the tick it was begun at.</summary>
        public int Now => _started ? _last : _begun;

        /// <summary>Says the world stands at <paramref name="tick" /> before its first tick is run.</summary>
        /// <remarks>Ignored once a tick has run, since the rota's clock is then its own.</remarks>
        public void Begin(int tick)
        {
            if (!_started)
                _begun = tick;
        }

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

            var kept = shift.Stored;
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

            var kept = shift.Stored;
            if (kept == null)
                return;

            kept.Period = 0;
            kept.Once = true;
            kept.At = tick;
        }

        /// <summary>Looks at a shift once on the next tick, before anything else due on it; a shift already waiting for that stays as it is.</summary>
        /// <remarks>For bringing something up to date with a change, so whatever runs on that tick runs against it as it now stands.</remarks>
        public void Next(Shift shift)
        {
            var tick = Now + 1;
            if (shift.List == _first && shift.At == tick)
                return;

            Leave(shift);
            shift.Once = true;
            shift.At = tick;
            Put(shift, _first);
        }

        /// <summary>Puts a shift back where what it keeps says it stood when the world was saved: on its turn, or due once on its tick.</summary>
        public void Restore(Shift shift)
        {
            var kept = shift.Stored;
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

            var kept = shift.Stored;
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
            Gather(_first);
            Gather(_each);
            Gather(_tens[Place(tick, Ten)]);
            Gather(_hundreds[Place(tick, Hundred)]);

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

        /// <summary>Adds every shift of <paramref name="list" /> to those due.</summary>
        /// <remarks>One by one: the framework the game runs copies a list added whole through an array of its own, on every tick.</remarks>
        void Gather(List<Shift> list)
        {
            for (var i = 0; i < list.Count; i++)
                _due.Add(list[i]);
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
