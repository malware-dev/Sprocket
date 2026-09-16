using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Runs everything, one tick at a time.
    /// </summary>
    /// <remarks>
    /// There is no build, no run and no stop. The system is always live, and edits
    /// take effect on the next tick - a sequence added while the ship is working
    /// simply begins working too. Removing the gap between writing something and
    /// seeing it happen is what turns "what will this do?" into something the user
    /// can answer by looking.
    /// </remarks>
    public sealed class AutomationRunner
    {
        /// <summary>
        /// One runner per sequence <em>per one of a system</em>.
        /// </summary>
        /// <remarks>
        /// Two airlocks are the same sequences run twice, each resolving the author's parts
        /// into its own doors, so each needs its own place to be mid-cycle. One runner per
        /// sequence meant the second of anything did nothing at all: the ones of a system were
        /// a name and a number that the runtime never saw.
        ///
        /// Keyed by both, packed, so the lookup stays a single dictionary hit on every tick.
        /// </remarks>
        readonly Dictionary<long, SequenceRunner> _runners = new Dictionary<long, SequenceRunner>();
        readonly List<long> _stale = new List<long>();
        readonly List<long> _live = new List<long>();
        readonly Automation _automation;
        readonly GridWorld _world;
        readonly Bindings _bindings = new Bindings();

        public AutomationRunner(Automation automation, GridWorld world)
        {
            _automation = automation;
            _world = world;
        }

        /// <summary>
        /// The game's current frame, as last reported.
        /// </summary>
        /// <remarks>
        /// Told to us rather than counted by us, and that distinction is load-bearing.
        /// Space Engineers wakes a mod every frame, every ten, or every hundred - and
        /// the slower two are not guaranteed to land on exact multiples, because when
        /// they fire depends on what else is in the game's component list.
        ///
        /// A runner counting its own wake-ups would therefore be measuring "how often
        /// I was woken", which is not time, and every duration in the system would
        /// drift with server load. Taking the frame number means elapsed is
        /// <c>now - then</c> and stays exact however irregularly we are called.
        /// </remarks>
        public int CurrentTick { get; private set; }

        /// <summary>
        /// What the program's declarations currently point at.
        /// </summary>
        /// <remarks>
        /// Owned here because this is the one place that has both the program and the world and is
        /// woken on a clock. Everything that needs to turn a name into a block reads it from here
        /// rather than resolving for itself, so the resolving happens once per change rather than
        /// once per asker.
        /// </remarks>
        public Bindings Bindings
        {
            get
            {
                // Checked on the way out rather than only on a tick, because the editor declares a
                // block and reads the sentence back in the same breath - and waiting for the next
                // frame would show it "something that's gone" for a block it just picked. Two
                // integer comparisons, so asking costs nothing when nothing moved.
                _bindings.Refresh(_world, _automation);
                return _bindings;
            }
        }

        /// <summary>Advances everything to the given frame.</summary>
        public void Tick(int tick)
        {
            CurrentTick = tick;
            Reconcile();

            // Two integer comparisons on an ordinary tick. See Bindings.Refresh.
            _bindings.Refresh(_world, _automation);


            for (var i = 0; i < _live.Count; i++)
            {
                SequenceRunner runner;
                if (_runners.TryGetValue(_live[i], out runner))
                    runner.Tick(_world, _bindings, CurrentTick);
            }
        }

        /// <summary>
        /// Live state for a sequence on the first of its system, or null if never ticked.
        /// </summary>
        /// <remarks>
        /// The editor shows a system's sequences, not one particular one of it, so it is shown
        /// the first. That is exactly right while a system has one, and a half-answer as soon
        /// as it has two - a screen that has to say which one it is talking about is part of
        /// the configurator work, not of this.
        /// </remarks>
        public SequenceRunner Get(Sequence sequence)
        {
            var system = sequence.System;
            var first = system != null && system.Instances.Count > 0
                ? system.Instances[0].Number
                : 0;

            SequenceRunner runner;
            return _runners.TryGetValue(Key(sequence.Id, first), out runner) ? runner : null;
        }

        /// <summary>Live state for a sequence on one particular one of its system.</summary>
        public SequenceRunner Get(Sequence sequence, SystemInstance instance)
        {
            SequenceRunner runner;
            var key = Key(sequence.Id, instance == null ? 0 : instance.Number);
            return _runners.TryGetValue(key, out runner) ? runner : null;
        }

        static long Key(int sequenceId, int instanceNumber)
        {
            return ((long)sequenceId << 20) | (uint)instanceNumber;
        }

        /// <summary>Starts a named sequence, as a trigger or another sequence would.</summary>
        public void Call(Sequence sequence)
        {
            if (sequence == null)
                return;

            var runner = Get(sequence);
            if (runner != null)
                runner.Call(_world, _bindings, CurrentTick);
        }

        /// <summary>
        /// Starts a sequence on one particular one of its system.
        /// </summary>
        /// <remarks>
        /// Which is what a button on the port airlock means, as against a button on the front one.
        /// The runners are already held per sequence and per one-of-a-system, so this is the same
        /// lookup the editor makes, used to fire rather than to read.
        /// </remarks>
        public void Call(Sequence sequence, SystemInstance instance)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Call(_world, _bindings, CurrentTick);
        }

        /// <summary>
        /// Keeps the set of runners matching the sequences and the ones of each system.
        /// </summary>
        /// <remarks>
        /// The live list is rebuilt here rather than walked from the systems on every tick,
        /// because ticking is the hot path and this only has to notice a change.
        /// </remarks>
        void Reconcile()
        {
            _live.Clear();

            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                var instances = system.Instances;
                var sequences = system.Sequences;

                for (var n = 0; n < instances.Count; n++)
                {
                    for (var q = 0; q < sequences.Count; q++)
                    {
                        var key = Key(sequences[q].Id, instances[n].Number);
                        _live.Add(key);

                        if (_runners.ContainsKey(key))
                            continue;

                        var runner = new SequenceRunner(sequences[q], instances[n]);

                        // What a run step reaches: the same system, the same one of it, by number.
                        var owner = system;
                        var one = instances[n];
                        runner.Calls = delegate(int number) { Call(owner.FindSequence(number), one); };

                        _runners.Add(key, runner);
                    }
                }
            }

            if (_runners.Count == _live.Count)
                return;

            _stale.Clear();
            foreach (var pair in _runners)
            {
                if (!_live.Contains(pair.Key))
                    _stale.Add(pair.Key);
            }

            for (var i = 0; i < _stale.Count; i++)
                _runners.Remove(_stale[i]);
        }
    }
}
