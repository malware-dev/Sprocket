using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
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
        readonly Dictionary<long, SequenceRunner> _runners = new Dictionary<long, SequenceRunner>();
        readonly List<long> _stale = new List<long>();
        readonly List<long> _live = new List<long>();
        readonly Automation _automation;
        readonly GridWorld _world;
        readonly Bindings _bindings = new Bindings();
        readonly Rota _rota;

        readonly bool _runsRota;

        bool _reshaped = true;
        int _checkedEdits = -1;
        int _checkedWiring = -1;
        int _checkedUnstarted = -1;
        int _checkedStarts = -1;
        int _lookedBindings = -1;
        int _lookedEdits = -1;
        int _lookedRunners = -1;

        /// <summary>
        /// A runner with a rota of its own, which it runs itself, for a program that shares its world
        /// with nothing else.
        /// </summary>
        public AutomationRunner(Automation automation, GridWorld world) : this(automation, world, new Rota())
        {
            _runsRota = true;
        }

        /// <summary>A runner on the world's rota, shared by every runner in it. See <see cref="Rota" />.</summary>
        public AutomationRunner(Automation automation, GridWorld world, Rota rota)
        {
            _automation = automation;
            _world = world;
            _rota = rota;
            Live = true;

            _automation.Reshaped += delegate { _reshaped = true; };
        }

        /// <summary>
        /// The game's current frame, as last reported.
        /// </summary>
        /// <remarks>
        /// Told rather than counted, and that distinction is load-bearing.
        /// Space Engineers wakes a mod every frame, every ten, or every hundred - and
        /// the slower two are not guaranteed to land on exact multiples, because when
        /// they fire depends on what else is in the game's component list.
        ///
        /// A runner counting its own wake-ups would therefore be measuring "how often
        /// I was woken", which is not time, and every duration in the system would
        /// drift with server load. Taking the frame number means elapsed is
        /// <c>now - then</c> and stays exact however irregularly the runner is woken.
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
        /// <summary>
        /// Whether the machine Sprocket runs on can run anything: its block powered and switched on.
        /// </summary>
        /// <remarks>
        /// Set by whatever owns the block, every tick. True by default, which is what a test and a
        /// paper target have - neither has a block to be unpowered.
        /// </remarks>
        public bool Live { get; set; }

        public void Tick(int tick)
        {
            CurrentTick = tick;

            // Two integer comparisons on an ordinary tick. See Bindings.Refresh.
            _bindings.Refresh(_world, _automation);

            Checking();

            // The sequences are not visited here: each is on the rota for what it is doing, and the
            // rota calls what is due. A world's rota is run by the session once every Sprocket in it
            // has ticked, so every runner's bindings are current when its sequences are called.
            if (_runsRota)
                _rota.Run(tick);
        }

        /// <summary>Takes everything this program put on the rota off it, when its Sprocket goes away.</summary>
        public void Retire()
        {
            foreach (var runner in _runners.Values)
                runner.Retire();
        }

        /// <summary>How many times the program has been checked.</summary>
        /// <remarks>Counted so it can be shown that a program nothing changes is checked once.</remarks>
        internal int Checks { get; private set; }

        /// <summary>How many times an open editor's look has looked at the construct.</summary>
        /// <remarks>Counted so it can be shown that an idle editor looks at nothing.</remarks>
        internal int Looks { get; private set; }

        /// <summary>How many times the runners have been matched to the program.</summary>
        /// <remarks>Counted so it can be shown that an ordinary tick matches nothing.</remarks>
        internal int Reconciled { get; private set; }

        /// <summary>The world this program runs in.</summary>
        internal GridWorld World => _world;

        /// <summary>The program being run.</summary>
        internal Automation Program => _automation;

        /// <summary>What the program's declarations point at, as last resolved on a tick.</summary>
        internal Bindings Current => _bindings;

        /// <summary>The rota this program's sequences are on.</summary>
        internal Rota Rota => _rota;

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
                runner.Call(_world, _bindings, CurrentTick, Live);
        }

        /// <summary>
        /// Starts a sequence on one particular one of its system.
        /// </summary>
        /// <remarks>
        /// Which is what a button on the port airlock means, as against a button on the front one.
        /// The runners are already held per sequence and per one-of-a-system, so this is the same
        /// lookup the editor makes, used to fire rather than to read.
        /// </remarks>
        public void Call(Sequence sequence, SystemInstance instance, bool byHand = false)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Call(_world, _bindings, CurrentTick, Live, byHand);
        }

        /// <summary>
        /// Abandons a run, as a button that stops a sequence does.
        /// </summary>
        /// <remarks>
        /// What was started stays started: a door told to open goes on opening, because Sprocket
        /// cannot un-invoke an action and pretending otherwise would be a promise the game does not
        /// keep. What stops is the sequence's own progress through its steps.
        /// <para>
        /// Nothing is refused here. Stopping a sequence that is not running is nothing happening, and
        /// a machine with no power is a machine whose sequences are held anyway.
        /// </para>
        /// </remarks>
        public void Halt(Sequence sequence, SystemInstance instance)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Stop();
        }

        /// <summary>
        /// Puts a saved run back, as soon as there is a runner to put it in.
        /// </summary>
        /// <remarks>
        /// Held rather than applied where the runner does not exist yet, which is the ordinary case:
        /// state is read as a world loads, and the runners are made on the first tick after that. A
        /// run held for a sequence that never appears is simply never applied, and costs one entry.
        /// </remarks>
        public void Resume(Sequence sequence, SystemInstance instance, int step, bool waiting, int waitTicks)
        {
            if (sequence == null)
                return;

            var key = Key(sequence.Id, instance == null ? 0 : instance.Number);

            SequenceRunner runner;
            if (_runners.TryGetValue(key, out runner))
            {
                runner.Resume(step, waiting, waitTicks);
                return;
            }

            var held = new Held();
            held.Which = key;
            held.Step = step;
            held.Waiting = waiting;
            held.WaitTicks = waitTicks;
            _resuming.Add(held);
        }

        /// <summary>One run read out of storage, waiting for its runner to be made.</summary>
        struct Held
        {
            public long Which;
            public int Step;
            public bool Waiting;
            public int WaitTicks;
        }

        readonly List<Held> _resuming = new List<Held>();

        /// <summary>Gives a freshly made runner whatever was read for it, if anything.</summary>
        void Resumed(long key, SequenceRunner runner)
        {
            for (var i = 0; i < _resuming.Count; i++)
            {
                if (_resuming[i].Which != key)
                    continue;

                var held = _resuming[i];
                _resuming.RemoveAt(i);
                runner.Resume(held.Step, held.Waiting, held.WaitTicks);
                return;
            }
        }

        /// <summary>
        /// Keeps the set of runners matching the sequences and the ones of each system.
        /// </summary>
        /// <remarks>
        /// Done on the tick after the program says it gained or lost something, and on no other: the
        /// program is what changes, so it is what says so. See <see cref="Automation.Reshaped" />.
        /// </remarks>
        void Reconcile()
        {
            Reconciled++;
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

                        var runner = new SequenceRunner(sequences[q], instances[n], this);

                        // What a run step reaches: the same system, the same one of it, by number.
                        var owner = system;
                        var one = instances[n];
                        runner.Calls = delegate(int number) { Call(owner.FindSequence(number), one); };

                        _runners.Add(key, runner);
                        Resumed(key, runner);
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
            {
                _runners[_stale[i]].Retire();
                _runners.Remove(_stale[i]);
            }
        }

        /// <summary>Matches the runners to the program where it gained or lost something, and checks it where it or its wiring changed; nothing otherwise.</summary>
        /// <remarks>
        /// A flag and four integer comparisons. Asked on every tick, and by an editor about to show what is wrong, so a
        /// screen built straight after an edit reads it checked.
        /// </remarks>
        public void Checking()
        {
            if (_reshaped)
            {
                _reshaped = false;
                Reconcile();

                // Runners just made have been told nothing yet.
                _checkedEdits = -1;
            }

            if (_automation.Edits != _checkedEdits || _automation.Wiring != _checkedWiring || _world.Unstarted != _checkedUnstarted
                || _automation.Starts != _checkedStarts)
            {
                Check();
            }
        }

        /// <summary>What the program says is wrong with each sequence, on every system and every blockset of a blueprint, with what cannot work switched off.</summary>
        /// <remarks>
        /// What a program can tell about itself without running, the way a compiler does: on the tick after it or its
        /// wiring changes, and on no other. What the construct has become since is found by a run, at the step it matters,
        /// and by an open editor for somebody looking.
        /// </remarks>
        void Check()
        {
            Checks++;
            _checkedEdits = _automation.Edits;
            _checkedWiring = _automation.Wiring;
            _checkedUnstarted = _world.Unstarted;
            _checkedStarts = _automation.Starts;

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
                        var runner = Get(sequences[q], instances[n]);
                        if (runner != null)
                            runner.Checked(SequenceSummary.Checked(_world, Bindings, sequences[q], instances[n]));
                    }
                }

                SwitchOffBroken(system);
            }
        }

        /// <summary>Switches off the system, or each one of a blueprint's, where the program says a sequence of it cannot work.</summary>
        /// <remarks>Faults only: a sequence nothing starts yet is not broken, and switching it off would make the step somebody is about to point at it fail for a second reason.</remarks>
        void SwitchOffBroken(AutomationSystem system)
        {
            var instances = system.Instances;
            var sequences = system.Sequences;

            for (var n = 0; n < instances.Count && system.Enabled; n++)
            {
                if (!instances[n].Enabled)
                    continue;

                for (var q = 0; q < sequences.Count; q++)
                {
                    var runner = Get(sequences[q], instances[n]);
                    if (runner == null || !runner.Found.Parks)
                        continue;

                    Faulted(sequences[q], instances[n], runner.Found);
                    break;
                }
            }
        }

        /// <summary>Switches off what <paramref name="sequence" /> runs on, with why, where it is switched on.</summary>
        /// <remarks>
        /// A logic system of its own as a whole, and one of a blueprint's alone, so the port airlock missing a door leaves
        /// the front one working. Never switched back on here: starting a machine again is somebody's decision.
        /// </remarks>
        internal void Faulted(Sequence sequence, SystemInstance instance, SequenceFault fault)
        {
            var system = sequence.System;
            if (system == null || !system.Enabled)
                return;

            var block = fault.Which == null || fault.Problem == SequenceProblem.EmptySlot
                ? fault.Which
                : _automation.Called(fault.Which, _world.Values);
            var halt = new Halt(fault.Problem, sequence.Number, fault.Step, fault.Because, block);

            if (system.IsBlueprint && instance != null)
            {
                if (!instance.Enabled)
                    return;

                instance.Enabled = false;
                instance.Halt = halt;
                return;
            }

            system.Enabled = false;
            system.Halt = halt;
        }

        /// <summary>Looks at the construct for each sequence, on every system and every blockset of a blueprint, for an open editor to show what is gone or may have been swapped.</summary>
        /// <remarks>
        /// Only for somebody looking, and nothing is switched off here: a run finds the same at the step that needs it. The
        /// blocks are looked at only where what the names point at, or the program, has changed since the last look, so an
        /// idle editor costs a pass over the runners asking which are waiting, however large the construct.
        /// </remarks>
        public void Look()
        {
            // What a step being waited on says about finishing changes without anything saying so, so it is asked each
            // look - of the few runs that are waiting, and of their blocks alone.
            foreach (var runner in _runners.Values)
                runner.Glance();

            var bindings = Bindings;
            if (bindings.Revision == _lookedBindings && _automation.Edits == _lookedEdits && Reconciled == _lookedRunners)
                return;

            Looks++;
            _lookedBindings = bindings.Revision;
            _lookedEdits = _automation.Edits;
            _lookedRunners = Reconciled;

            foreach (var runner in _runners.Values)
                runner.Looked = SequenceSummary.Looked(bindings, runner.Sequence, runner.Instance);
        }
    }
}
