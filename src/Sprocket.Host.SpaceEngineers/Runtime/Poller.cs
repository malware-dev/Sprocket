using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Watches one <c>when</c> or <c>every</c> of a system on one of its instances, and does its step when it fires.
    /// </summary>
    /// <remarks>
    /// On the rota at its own cadence, so a moment looked for every half second costs a look every half second and nothing
    /// between. The step is done by the runner of the whole program, which knows what a step reaches.
    /// <para>
    /// <b>Still looked at while the machine has no power,</b> and nothing done. An edge compares a look with the one
    /// before it, so a door opened during a blackout must not read as having just opened when the lights come back.
    /// </para>
    /// <para>
    /// <b>A moment every watched block announces is looked for when it is announced,</b> on the tick after, with a look
    /// every hundred ticks in case an announcement never comes; how often the program asked for is set aside. What is
    /// announced only prompts the look, which reads the blocks as ever.
    /// </para>
    /// <para>
    /// <b>So is a moment ruled out on every watched block,</b> one the block does not announce but which cannot happen
    /// while a reading it does announce holds: a door cannot start closing while it is shut. Once a block is out of that
    /// reading, the moment is looked for on its cadence until the block is back in it. See
    /// <see cref="EntityEvent.RuledOutBy" />.
    /// </para>
    /// </remarks>
    public sealed class Poller
    {
        /// <summary>How often a moment is looked for where the program leaves it out: <c>10t</c>.</summary>
        const int Default = Rota.Ten;

        /// <summary>How often an announced moment is looked for all the same, in case an announcement never comes.</summary>
        const int Backstop = Rota.Hundred;

        readonly AutomationSystem _system;
        readonly Trigger _trigger;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly Rota _rota;
        readonly Shift _shift;

        readonly Dictionary<string, bool> _kept = new Dictionary<string, bool>();
        readonly List<string> _watched = new List<string>();
        readonly List<IDisposable> _listening = new List<IDisposable>();
        readonly Action _heard;

        string[] _names = new string[0];
        GridEntity[] _entities = new GridEntity[0];
        Probe[] _probes = new Probe[0];
        Recall[] _ruling = new Recall[0];
        bool[] _previous = new bool[0];
        bool[] _known = new bool[0];

        int _compiledBindings = -1;
        int _compiledEdits = -1;
        bool _announced;
        bool _possible;

        int _all = -1;
        int _due = -1;
        int _since = -1;
        int _tick;
        Bindings _bindings;

        /// <summary>A watch on <paramref name="trigger" /> of <paramref name="system" />, on <paramref name="instance" />.</summary>
        public Poller(AutomationSystem system, Trigger trigger, SystemInstance instance, AutomationRunner owner)
        {
            _system = system;
            _trigger = trigger;
            _instance = instance;
            _owner = owner;
            _rota = owner.Rota;
            _shift = new Shift(_rota.Join(), Look);
            _heard = Heard;
            _rota.At(_shift, int.MinValue);
        }

        /// <summary>The trigger watched.</summary>
        public Trigger Trigger => _trigger;

        /// <summary>The instance whose blocks it watches.</summary>
        public SystemInstance Instance => _instance;

        /// <summary>Whether the comparison it watches held at the last look, on any block or on what it asks of them all; false until the first look.</summary>
        public bool Live { get; private set; }

        /// <summary>Whether one or more of the blocks it watches is no longer on the construct.</summary>
        public bool HasMissingSources { get; private set; }

        /// <summary>How many times it has fired.</summary>
        public int FiredCount { get; private set; }

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
                    asked = when.IntervalTicks > 0 ? when.IntervalTicks : Default;
                }
                else
                {
                    var every = _trigger as EveryTrigger;
                    asked = every == null || every.IntervalTicks < 1 ? Default : every.IntervalTicks;
                }

                return asked < _owner.QuickestLook ? _owner.QuickestLook : asked;
            }
        }

        /// <summary>Whether the comparison held on each block at its last look, by the name the program gives the block; for a save.</summary>
        public IEnumerable<KeyValuePair<string, bool>> Signals
        {
            get
            {
                for (var i = 0; i < _names.Length; i++)
                {
                    if (_known[i])
                        yield return new KeyValuePair<string, bool>(_names[i], _previous[i]);
                }

                foreach (var kept in _kept)
                    yield return kept;
            }
        }

        /// <summary>Whether the comparison held on every block, or on their sum, at the last look of a <c>when all</c> or a <c>when sum</c>: 1 or 0, or -1 before its first look.</summary>
        public int All => _all;

        /// <summary>Ticks since an <c>every</c> last fell due, or -1 before its first look.</summary>
        public int Since => _due < 0 ? -1 : _owner.CurrentTick - _due;

        /// <summary>Takes back what a save says it read of <paramref name="block" /> at its last look before the world was written.</summary>
        public void Recall(string block, bool was)
        {
            for (var i = 0; i < _names.Length; i++)
            {
                if (_names[i] == block)
                {
                    _previous[i] = was;
                    _known[i] = true;
                    return;
                }
            }

            _kept[block] = was;
        }

        /// <summary>Takes back whether every block was there at the last look before the world was written.</summary>
        public void RecallAll(bool all)
        {
            _all = all ? 1 : 0;
        }

        /// <summary>Takes back how long it had been since an <c>every</c> last fell due when the world was written.</summary>
        /// <remarks>Counted on from the first look, since the world's clock begins again at a load.</remarks>
        public void RecallSince(int ticks)
        {
            _since = ticks;
        }

        /// <summary>Takes it off the rota for good, when its trigger or its instance no longer exists.</summary>
        public void Retire()
        {
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
            else if (_trigger is EveryTrigger && _due >= 0)
                // When it falls due rather than a whole cadence on, which is the same tick except after a load, where it
                // takes up the beat it had.
                _rota.At(_shift, Math.Max(_tick + 1, _due + cadence));
            else
                _rota.At(_shift, _tick + cadence);
        }

        /// <summary>A block announced a change: looks on the next tick, and no sooner than the runner allows after the last look.</summary>
        void Heard()
        {
            _rota.At(_shift, _tick + _owner.QuickestLook);
        }

        /// <summary>
        /// Works out which blocks it watches, what each is read by, and listens to every one that announces a change in what
        /// it reads or when that is ruled out, marking whether all of them do.
        /// </summary>
        /// <remarks>
        /// Done again only where the blocks the names point at, or the program, have changed since, so a look reads arrays
        /// and asks nothing by name. What it read of a block at its last look is kept across, by the block's name, and let
        /// go of for any block it no longer watches.
        /// </remarks>
        void Compile()
        {
            var when = _trigger as WhenTrigger;
            var signal = _trigger as SignalTrigger;
            var watched = when != null ? (MomentTrigger)when : signal;
            if (watched == null || (_bindings.Revision == _compiledBindings && _owner.Program.Edits == _compiledEdits))
                return;

            _compiledBindings = _bindings.Revision;
            _compiledEdits = _owner.Program.Edits;
            Deafen();

            for (var i = 0; i < _names.Length; i++)
            {
                if (_known[i])
                    _kept[_names[i]] = _previous[i];
            }

            Watched(watched.Sources);
            var count = _watched.Count;
            _names = new string[count];
            _entities = new GridEntity[count];
            _probes = new Probe[count];
            _ruling = new Recall[count];
            _previous = new bool[count];
            _known = new bool[count];

            var all = count > 0;
            for (var i = 0; i < count; i++)
            {
                var name = _watched[i];
                var entity = _bindings.Entity(name);
                var probe = when != null
                    ? Probe.Of(entity, when.Provider, when.EventId, when.Arguments, when.Quantifier, when.Relation, when.Value)
                    : Probe.Watching(entity, signal.Provider, signal.EventId);

                var ruling = probe == null ? null : probe.RuledOutBy;
                _names[i] = name;
                _entities[i] = entity;
                _probes[i] = probe;
                _ruling[i] = ruling != null && ruling.Remembers ? new Recall() : null;

                bool was;
                if (_kept.TryGetValue(name, out was))
                {
                    _previous[i] = was;
                    _known[i] = true;
                }

                var heard = probe != null && (probe.Announced || ruling != null);
                var listening = heard ? entity.Listen(_heard) : null;
                if (listening == null)
                {
                    all = false;
                    continue;
                }

                _listening.Add(listening);
            }

            _kept.Clear();
            _announced = all;
        }

        /// <summary>Stops listening to every block.</summary>
        void Deafen()
        {
            for (var i = 0; i < _listening.Count; i++)
                _listening[i].Dispose();

            _listening.Clear();
            _announced = false;
        }

        /// <summary>How many times it fires on this look.</summary>
        int Sampled()
        {
            _possible = false;
            var when = _trigger as WhenTrigger;
            if (when != null && when.Quantifier == Quantifier.Sum)
                return SampleSum(when) ? 1 : 0;

            if (when != null)
                return when.Quantifier == Quantifier.All ? (SampleWhenAll() ? 1 : 0) : SampleWhen();

            if (_trigger is SignalTrigger)
                return SampleWhen();

            var every = _trigger as EveryTrigger;
            return every != null && SampleEvery(every) ? 1 : 0;
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
            var anyMissing = false;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    anyMissing = true;
                    continue;
                }

                var probe = _probes[i];
                if (probe == null)
                    continue;

                Mark(probe, _ruling[i]);
                var now = probe.Holds();
                if (now)
                    anyTrue = true;

                // No previous look means the trigger has only just come into existence, and an edge needs two.
                // Firing on the first look would set every trigger off the moment it was made.
                if (_known[i] && !_previous[i] && now)
                    fired++;

                _previous[i] = now;
                _known[i] = true;
            }

            Live = anyTrue;
            Missed(anyMissing);
            return fired;
        }

        /// <summary>Notes where the block <paramref name="probe" /> reads could change unannounced: one whose change it does not announce, and nothing rules out now.</summary>
        void Mark(Probe probe, Recall ruling)
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
        /// <remarks>
        /// Watching for several separate edges on the same tick would almost never fire, because the blocks reach their
        /// state at different moments. What the sentence means is "the moment they are all there", so that is what is
        /// watched: one edge, on the whole condition.
        /// </remarks>
        bool SampleWhenAll()
        {
            var all = _entities.Length > 0;
            var anyMissing = false;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    anyMissing = true;
                    all = false;
                    continue;
                }

                var probe = _probes[i];
                if (probe == null)
                {
                    all = false;
                    continue;
                }

                Mark(probe, _ruling[i]);
                if (!probe.Holds())
                    all = false;
            }

            Live = all;
            Missed(anyMissing);
            return Edge(all);
        }

        /// <summary>
        /// Fires when the comparison comes to hold on the sum of what the blocks read, and not again until it stops holding.
        /// </summary>
        /// <remarks>Every block is read, so a block the sum cannot read leaves it out of reach: it never holds.</remarks>
        bool SampleSum(WhenTrigger when)
        {
            var total = 0d;
            var readable = _entities.Length > 0;
            var anyMissing = false;

            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null)
                {
                    anyMissing = true;
                    readable = false;
                    continue;
                }

                var probe = _probes[i];
                if (probe == null)
                {
                    readable = false;
                    continue;
                }

                Mark(probe, _ruling[i]);
                total += probe.Number();
            }

            var holds = readable && Relations.Holds(Literal.Of(total), when.Relation, when.Value);
            Live = holds;
            Missed(anyMissing);
            return Edge(holds);
        }

        /// <summary>Whether the whole condition has just come to hold, which is unknown until the second look, so a condition already true when the trigger appears does not set it off.</summary>
        bool Edge(bool holds)
        {
            var now = holds ? 1 : 0;
            var fired = _all == 0 && now == 1;
            _all = now;
            return fired;
        }

        /// <remarks>
        /// Measured in the game's ticks rather than in looks. The next time falls due from when the last one did, not from
        /// when it was noticed, so a clock noticed a few ticks late runs late by the same few ticks every time rather than
        /// drifting further each time.
        /// </remarks>
        bool SampleEvery(EveryTrigger every)
        {
            var interval = every.IntervalTicks < 1 ? 1 : every.IntervalTicks;
            if (_due < 0 && _since >= 0)
            {
                _due = _tick - _since;
                _since = -1;
            }

            if (_due < 0 || _tick < _due)
            {
                _due = _tick;
                return false;
            }

            if (_tick - _due < interval)
                return false;

            // A world that was paused, or a clock that was not looked at, starts counting again rather than firing once
            // for every period it missed.
            _due = _tick - _due < 2 * interval ? _due + interval : _tick;
            return true;
        }

        /// <summary>Keeps whether a block it watches is gone, telling the runner where that changed.</summary>
        void Missed(bool missing)
        {
            if (HasMissingSources == missing)
                return;

            HasMissingSources = missing;
            _owner.MissingChanged();
        }
    }
}
