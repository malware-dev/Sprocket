using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// Watches one <c>when</c> or <c>every</c> of a system on one of its instances, and does its step when it fires.
    /// </summary>
    /// <remarks>
    /// On the rota at its own cadence, and still looked at while the machine has no power, with nothing done. A moment
    /// every watched block announces, or rules out, is looked for on the tick after it is announced, and every hundred
    /// ticks in case an announcement never comes.
    /// </remarks>
    public sealed class Poller
    {
        const int Default = Rota.Ten;

        const int Backstop = Rota.Hundred;

        readonly AutomationSystem _system;
        readonly Trigger _trigger;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly Rota _rota;
        readonly Shift _shift;

        // Kept in what the world saves.
        readonly WatchMemory _memory;

        readonly List<string> _watched = new List<string>();
        readonly List<IDisposable> _listening = new List<IDisposable>();
        readonly Action _heard;

        GridEntity[] _entities = new GridEntity[0];
        Probe[] _probes = new Probe[0];
        ReadingMemory[] _reads = new ReadingMemory[0];
        int _generation;

        // What a when compares with and gives its member on this installation, each setting standing there given its value.
        Literal _value;
        IList<Literal> _arguments;

        // The length of the setting a trigger looks or starts as often as, in ticks, and the program's edit it was looked up at.
        int _intervalTicks;
        int _intervalEdits = int.MinValue;

        int _compiledBindings = -1;
        int _compiledEdits = -1;
        int _compiledNamespaces = -1;
        bool _announced;
        bool _possible;

        int _tick;
        Bindings _bindings;

        /// <summary>A watch on <paramref name="trigger" /> of <paramref name="system" />, on <paramref name="instance" />, remembering in <paramref name="memory" />.</summary>
        /// <remarks>Looks on the first tick, which is where it starts to listen, and a watch the world remembers compares with what it read.</remarks>
        public Poller(AutomationSystem system, Trigger trigger, SystemInstance instance, AutomationRunner owner, WatchMemory memory)
        {
            _system = system;
            _trigger = trigger;
            _instance = instance;
            _owner = owner;
            _rota = owner.Rota;
            _memory = memory;
            _heard = Heard;

            var kept = memory.Shift != null;
            if (!kept)
                memory.Shift = new ShiftMemory { Place = _rota.Join() };

            _shift = new Shift(memory.Shift, Look);

            // A block's announcements while the world was away were heard by nobody, so a remembered watch looks at once too.
            _rota.At(_shift, int.MinValue);
        }

        /// <summary>What it remembers, which the world's save writes.</summary>
        public WatchMemory Memory => _memory;

        /// <summary>The trigger watched.</summary>
        public Trigger Trigger => _trigger;

        /// <summary>The instance whose blocks it watches.</summary>
        public SystemInstance Instance => _instance;

        /// <summary>Whether the comparison it watches held at the last look, on any block or on what it asks of them all; false until the first look.</summary>
        public bool Live
        {
            get { return _memory.Live; }
            private set { _memory.Live = value; }
        }

        /// <summary>The first of the blocks it watches that is no longer on the construct, by its reference, or null.</summary>
        public string Lost { get; private set; }

        /// <summary>How many times it has fired.</summary>
        public int FiredCount
        {
            get { return _memory.Fired; }
            private set { _memory.Fired = value; }
        }

        /// <summary>
        /// How often it looks, in ticks: what the program says, or <c>10t</c> where a <c>when</c> leaves it out, and never more
        /// often than the runner allows.
        /// </summary>
        public int Cadence
        {
            get
            {
                int asked;
                var when = _trigger as WhenTrigger;
                if (when != null)
                {
                    var ticks = IntervalTicks();
                    asked = ticks > 0 ? ticks : Default;
                }
                else
                {
                    var every = _trigger as EveryTrigger;
                    var ticks = IntervalTicks();
                    asked = every == null || ticks < 1 ? Default : ticks;
                }

                return asked < _owner.FastestUpdate ? _owner.FastestUpdate : asked;
            }
        }

        /// <summary>How often it looks, or how long between starts, in ticks: what the program says, or the length the setting it names has on this installation; 0 where neither says.</summary>
        /// <remarks>Asked on each look; a setting is looked up again only once the program has changed.</remarks>
        int IntervalTicks()
        {
            var when = _trigger as WhenTrigger;
            var every = _trigger as EveryTrigger;
            var setting = when != null ? when.IntervalSetting : every != null ? every.IntervalSetting : "";
            if (setting.Length == 0)
                return when != null ? when.IntervalTicks : every != null ? every.IntervalTicks : 0;

            var edits = _owner.Program.Edits;
            if (edits == _intervalEdits)
                return _intervalTicks;

            _intervalEdits = edits;
            Literal given;
            _intervalTicks = _instance != null && _instance.Gives(setting, out given) && given.Kind == ValueKind.Duration ? given.Length.Ticks : 0;
            return _intervalTicks;
        }

        /// <summary>What it remembers of <paramref name="block" />, by the name the program gives it; a fresh memory where it remembers nothing.</summary>
        ReadingMemory Reading(string block)
        {
            var reads = _memory.Readings;
            foreach (var reading in reads)
            {
                if (reading.Block == block)
                    return reading;
            }

            return new ReadingMemory { Block = block };
        }

        /// <summary>Takes it off the rota for good, when its trigger, its instance or its Sprocket no longer exists, leaving what it remembers as it was.</summary>
        public void Retire()
        {
            _shift.Forget();
            _rota.Leave(_shift);
            Deafen();
        }

        void Look(int tick)
        {
            _tick = tick;
            _bindings = _owner.Current;
            Compile();

            var firings = Sampled();
            for (var i = 0; i < firings && _owner.Live; i++)
            {
                FiredCount++;
                _owner.Fire(_system, _trigger, _instance);
            }

            Schedule();
        }

        /// <summary>Puts the next look on the rota: on its place where the cadence is one the rota keeps, and on the tick it falls due otherwise.</summary>
        void Schedule()
        {
            var cadence = _announced && !_possible && Cadence < Backstop ? Backstop : Cadence;
            if (cadence == Rota.Each || cadence == Rota.Ten || cadence == Rota.Hundred)
                _rota.Every(_shift, cadence);
            else if (_trigger is EveryTrigger && _memory.Due >= 0)
                // When it falls due rather than a whole cadence on, which is the same tick except where a copy was sent its beat.
                _rota.At(_shift, Math.Max(_tick + 1, _memory.Due + cadence));
            else
                _rota.At(_shift, _tick + cadence);
        }

        /// <summary>A block announced a change: looks on the next tick, which the world's fastest update does not hold back, as nothing repeats.</summary>
        void Heard()
        {
            _rota.At(_shift, _tick + 1);
        }

        /// <summary>
        /// Works out which blocks it watches, what each is read by, and listens to every one that announces a change in what
        /// it reads or when that is ruled out, marking whether all of them do.
        /// </summary>
        /// <remarks>
        /// Done again only where the blocks the names point at, the program, or the host's namespaces have changed since, so a
        /// look reads arrays and asks nothing by name.
        /// </remarks>
        void Compile()
        {
            var when = _trigger as WhenTrigger;
            var signal = _trigger as SignalTrigger;
            var watched = when != null ? (MomentTrigger)when : signal;
            if (watched == null || (_bindings.Revision == _compiledBindings && _owner.Program.Edits == _compiledEdits
                    && HostNamespaces.Changes == _compiledNamespaces))
                return;

            _compiledBindings = _bindings.Revision;
            _compiledEdits = _owner.Program.Edits;
            _compiledNamespaces = HostNamespaces.Changes;
            Deafen();
            _generation++;

            if (when != null)
            {
                SystemInstance.Settled(_instance, when.Value, out _value);
                _arguments = SystemInstance.Settled(_instance, when.Arguments);
            }

            Watched(watched.Sources);
            var count = _watched.Count;
            _entities = new GridEntity[count];
            _probes = new Probe[count];
            var reads = new ReadingMemory[count];

            var all = count > 0;
            for (var i = 0; i < count; i++)
            {
                var name = _watched[i];
                var entity = _bindings.Entity(name);
                var read = Reading(name);
                if (_generation > 1)
                {
                    read.Ruling = null;
                    read.Seen = null;
                    read.Gauge = null;
                }

                var probe = when != null
                    ? Probe.Of(entity, when.Key, _arguments, when.Quantifier, when.Relation, _value, read,
                        _owner.Clock, _instance == null ? 0 : _instance.FacetOf(when.Sources, name))
                    : Probe.Watching(entity, signal.Key, read, _owner.Clock);

                var ruling = probe?.RuledOutBy;
                _entities[i] = entity;
                _probes[i] = probe;
                if (ruling == null || !ruling.Remembers)
                    read.Ruling = null;
                else if (read.Ruling == null)
                    read.Ruling = new ReaderMemory { Clock = _owner.Clock };
                else
                    read.Ruling.Clock = _owner.Clock;

                reads[i] = read;

                var heard = probe != null && (probe.Announced || ruling != null);
                var listening = heard ? entity.Listen(_heard) : null;
                if (listening == null)
                {
                    all = false;
                    continue;
                }

                _listening.Add(listening);
            }

            _reads = reads;
            _memory.Readings.Clear();
            _memory.Readings.AddRange(reads);
            _announced = all;
        }

        /// <summary>Stops listening to every block.</summary>
        void Deafen()
        {
            foreach (var listener in _listening)
                listener.Dispose();

            _listening.Clear();
            _announced = false;
        }

        /// <summary>How many times it fires on this look.</summary>
        int Sampled()
        {
            _possible = false;
            var when = _trigger as WhenTrigger;
            if (when != null && (when.Quantifier == Quantifier.Sum || when.Quantifier == Quantifier.Average))
                return SampleSum(when) ? 1 : 0;

            if (when != null)
                return when.Quantifier == Quantifier.All ? (SampleWhenAll() ? 1 : 0) : SampleWhen();

            if (_trigger is SignalTrigger)
                return SampleWhen();

            var every = _trigger as EveryTrigger;
            return every != null && SampleEvery() ? 1 : 0;
        }

        /// <summary>Works out the blocks <paramref name="slots" /> come to, each once.</summary>
        void Watched(IList<string> slots)
        {
            _watched.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < slots.Count; i++)
                    _watched.Add(slots[i]);
            }
            else
            {
                _instance.Resolve(slots, _watched);
            }

            _bindings.Distinct(_watched);
        }

        /// <summary>Fires once for each block the comparison has come to hold on since the last look, however close together they arrived.</summary>
        int SampleWhen()
        {
            var fired = 0;
            var anyTrue = false;
            string lost = null;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    lost = lost ?? _watched[i];
                    continue;
                }

                var probe = _probes[i];
                if (probe == null || probe.PassedOver)
                    continue;

                Mark(probe, _reads[i].Ruling);
                var now = probe.Holds();
                if (now)
                    anyTrue = true;

                // An edge needs two looks, so a trigger never fires on its first.
                if (_reads[i].Known && !_reads[i].Held && now)
                    fired++;

                _reads[i].Held = now;
                _reads[i].Known = true;
            }

            Live = anyTrue;
            Missed(lost);
            return fired;
        }

        /// <summary>Notes where the block <paramref name="probe" /> reads could change unannounced: one whose change it does not announce, and nothing rules out now.</summary>
        void Mark(Probe probe, ReaderMemory ruling)
        {
            if (_possible || probe.Announced)
                return;

            if (probe.RuledOutBy == null || !probe.Entity.Reads(probe.RuledOutBy, ruling))
                _possible = true;
        }

        /// <summary>
        /// Fires when the comparison comes to hold on the last of the blocks, and not again until it stops holding on one of
        /// them.
        /// </summary>
        bool SampleWhenAll()
        {
            var all = _entities.Length > 0;
            string lost = null;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    lost = lost ?? _watched[i];
                    all = false;
                    continue;
                }

                var probe = _probes[i];
                if (probe == null)
                {
                    all = false;
                    continue;
                }

                if (probe.PassedOver)
                    continue;

                Mark(probe, _reads[i].Ruling);
                if (!probe.Holds())
                    all = false;
            }

            Live = all;
            Missed(lost);
            return Edge(all);
        }

        /// <summary>
        /// Fires when the comparison comes to hold on the sum of what the blocks read, and not again until it stops holding.
        /// </summary>
        /// <remarks>
        /// Every block is read, so a block the sum cannot read leaves it out of reach: it never holds. Over no blocks the sum
        /// is 0 and the average never holds.
        /// </remarks>
        bool SampleSum(WhenTrigger when)
        {
            var total = 0d;
            var read = 0;
            var readable = _entities.Length > 0 || when.Quantifier == Quantifier.Sum;
            string lost = null;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    lost = lost ?? _watched[i];
                    readable = false;
                    continue;
                }

                var probe = _probes[i];
                if (probe == null)
                {
                    readable = false;
                    continue;
                }

                if (probe.PassedOver)
                    continue;

                read++;
                Mark(probe, _reads[i].Ruling);
                total += probe.Number();
            }

            // Over the blocks that answered: none of them leaves no number to give.
            if (when.Quantifier == Quantifier.Average)
            {
                readable = readable && read > 0;
                if (read > 0)
                    total /= read;
            }

            var holds = readable && Relations.Holds(Literal.Of(total), when.Relation, _value);
            Live = holds;
            Missed(lost);
            return Edge(holds);
        }

        /// <summary>Whether the whole condition has just come to hold, which is unknown until the second look, so a condition already true when the trigger appears does not set it off.</summary>
        bool Edge(bool holds)
        {
            var now = holds ? 1 : 0;
            var fired = _memory.All == 0 && now == 1;
            _memory.All = now;
            return fired;
        }

        /// <remarks>
        /// Measured in the game's ticks; the next time falls due from when the last one did, so a clock noticed late does not
        /// drift.
        /// </remarks>
        bool SampleEvery()
        {
            var ticks = IntervalTicks();
            var interval = ticks < 1 ? 1 : ticks;
            if (_memory.Due < 0 || _tick < _memory.Due)
            {
                _memory.Due = _tick;
                return false;
            }

            if (_tick - _memory.Due < interval)
                return false;

            // A world that was paused, or a clock that was not looked at, starts counting again rather than firing once
            // for every period it missed.
            _memory.Due = _tick - _memory.Due < 2 * interval ? _memory.Due + interval : _tick;
            return true;
        }

        void Missed(string lost)
        {
            if (Lost == lost)
                return;

            Lost = lost;
            _owner.Watches.MissingChanged();
        }
    }
}
