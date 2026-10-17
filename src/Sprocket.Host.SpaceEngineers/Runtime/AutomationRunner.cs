using System;
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
        readonly Runs _runs;
        readonly Watches _watches;
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
            _runs = new Runs(this, automation);
            _watches = new Watches(this, automation);
            Live = true;
            Clock = () => CurrentTick;

            _automation.Reshaped += () => _reshaped = true;
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

        /// <summary>Sprocket's clock as the readers of blocks are given it, which tells them the tick they read on.</summary>
        public Func<int> Clock { get; }

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
                Refreshed();
                return _bindings;
            }
        }

        /// <summary>
        /// Whether the machine Sprocket runs on can run anything: its block powered and switched on.
        /// </summary>
        /// <remarks>
        /// Set by whatever owns the block, every tick. True by default, which is what a test and a
        /// paper target have - neither has a block to be unpowered.
        /// </remarks>
        public bool Live { get; set; }

        /// <summary>The fewest ticks between two runs of anything on a repeat, whatever the program asks for: a timer, a <c>when</c>'s look, a run's look at a step it waits on.</summary>
        /// <remarks>
        /// Set by whatever owns the runner, from the world's fastest update; 1 by default, which holds nothing back. A change the
        /// game announces and a timed wait are not on a repeat, and keep their own moment.
        /// </remarks>
        public int FastestUpdate { get; set; } = 1;

        /// <summary>Whether a screen is open on this construct, which is who <see cref="Look" /> is for.</summary>
        /// <remarks>Set by the screen as it opens and closes.</remarks>
        public bool Looking { get; set; }

        /// <summary>Advances everything to the given frame.</summary>
        public void Tick(int tick)
        {
            CurrentTick = tick;

            // Two integer comparisons on an ordinary tick. See Bindings.Refresh.
            Refreshed();

            Checking();

            // The sequences are not visited here: each is on the rota for what it is doing, and the
            // rota calls what is due. A world's rota is run by the session once every Sprocket in it
            // has ticked, so every runner's bindings are current when its sequences are called.
            if (_runsRota)
                _rota.Run(tick);
        }

        /// <summary>Starts the clock at <paramref name="tick" />, which is where a world just loaded stands before its first tick.</summary>
        public void Begin(int tick)
        {
            CurrentTick = tick;
        }

        /// <summary>Makes the runners and checks the program now, rather than on the next tick, for a press that arrives first.</summary>
        public void Prepare()
        {
            Refreshed();
            Checking();
        }

        /// <summary>Takes everything this program put on the rota off it, when its Sprocket goes away.</summary>
        public void Retire()
        {
            _runs.Retire();
            _watches.Retire();
        }

        /// <summary>How many times the program has been checked.</summary>
        /// <remarks>Counted so it can be shown that a program nothing changes is checked once.</remarks>
        public int Checks { get; private set; }

        /// <summary>How many times an open editor's look has looked at the construct.</summary>
        /// <remarks>Counted so it can be shown that an idle editor looks at nothing.</remarks>
        public int Looks { get; private set; }

        /// <summary>How many times the runners have been matched to the program.</summary>
        /// <remarks>Counted so it can be shown that an ordinary tick matches nothing.</remarks>
        public int Reconciled { get; private set; }

        /// <summary>Counts up on the tick anything an open screen shows of the runs may have changed: a fault found or cleared, a run held or let go, a stop, a halt, the runners themselves.</summary>
        /// <remarks>Read by an open page on every tick, which recounts its rows only where it moved.</remarks>
        public int Revision { get; private set; }

        /// <summary>The world this program runs in.</summary>
        public GridWorld World => _world;

        /// <summary>The program being run.</summary>
        public Automation Program => _automation;

        /// <summary>What the program's declarations point at, as last resolved on a tick.</summary>
        public Bindings Current => _bindings;

        /// <summary>The rota this program's sequences are on.</summary>
        public Rota Rota => _rota;

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
            var first = system != null && system.Uses.Count > 0
                ? system.Uses[0].Number
                : 0;

            return _runs.Get(Runs.Key(sequence.Id, first));
        }

        /// <summary>Live state for a sequence on one particular one of its system.</summary>
        public SequenceRunner Get(Sequence sequence, SystemInstance instance)
        {
            return _runs.Get(Runs.Key(sequence.Id, instance?.Number ?? 0));
        }

        /// <summary>
        /// What runs the act a trigger does itself: a sequence with no number and that act for its one step, or null
        /// where the trigger runs or stops a sequence instead.
        /// </summary>
        public Sequence Alone(AutomationSystem system, Trigger trigger)
        {
            return _runs.Alone(system, trigger);
        }

        /// <summary>The runners of the program.</summary>
        public Runs Runs => _runs;

        /// <summary>The pollers of the program.</summary>
        public Watches Watches => _watches;

        /// <summary>Says something an open screen shows of the runs has changed, on the tick it changed.</summary>
        public void Revise()
        {
            Revision++;
        }

        /// <summary>Does what <paramref name="trigger" /> of <paramref name="system" /> does on <paramref name="instance" />, because what it watches for came round.</summary>
        /// <remarks>
        /// A sequence switched off lets it go by, and one already running counts it as skipped; see
        /// <see cref="SequenceRunner.Triggered" />. A stop is nothing happening where the sequence is not running.
        /// </remarks>
        public void Fire(AutomationSystem system, Trigger trigger, SystemInstance instance)
        {
            var run = trigger.Does as Run;
            if (run != null)
            {
                var sequence = system.FindSequence(run.Number);
                if (sequence == null)
                    return;

                if (run.Stops)
                {
                    Halt(sequence, instance, Stopper.Trigger, 0);
                    return;
                }

                var runner = Get(sequence, instance);
                if (runner != null)
                    runner.Triggered(_bindings, CurrentTick);

                return;
            }

            var alone = Alone(system, trigger);
            var acting = alone == null ? null : Get(alone, instance);
            if (acting != null)
                acting.Triggered(_bindings, CurrentTick);
        }

        /// <summary>Starts a named sequence, as a trigger or another sequence would.</summary>
        public void Call(Sequence sequence)
        {
            if (sequence == null)
                return;

            var runner = Get(sequence);
            if (runner != null)
                runner.Call(_bindings, CurrentTick, Live);
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
                runner.Call(_bindings, CurrentTick, Live, byHand);
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
        public void Halt(Sequence sequence, SystemInstance instance, Stopper by, int from)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Interrupt(by, from);
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
            Revise();
            _runs.Reconcile();
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

            Match();

            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                var instances = system.Uses;
                var sequences = system.Sequences;

                var triggers = system.Triggers;

                for (var n = 0; n < instances.Count; n++)
                {
                    for (var q = 0; q < sequences.Count; q++)
                    {
                        var runner = Get(sequences[q], instances[n]);
                        if (runner != null)
                            runner.Faults.Checked(SequenceSummary.Checked(_world, Bindings, sequences[q], instances[n]));
                    }

                    for (var t = 0; t < triggers.Count; t++)
                    {
                        var alone = Alone(system, triggers[t]);
                        var runner = alone == null ? null : Get(alone, instances[n]);
                        if (runner != null)
                            runner.Faults.Checked(SequenceSummary.Checked(_world, Bindings, alone, instances[n]));
                    }
                }

                SwitchOffBroken(system);
            }

            // The program, its wiring or its runners changed, which is when what an open screen shows of the blocks it
            // names may have.
            if (Looking)
                Look();
        }

        /// <summary>Works out what the declarations point at where the blocks or the program changed, and looks for an open screen when they did.</summary>
        void Refreshed()
        {
            if (_bindings.Refresh(_world, _automation) && Looking)
                Look();
        }

        /// <summary>Switches off the system, or each one of a blueprint's, where the program says a sequence of it cannot work.</summary>
        /// <remarks>Faults only: a sequence nothing starts yet is not broken, and switching it off would make the step somebody is about to point at it fail for a second reason.</remarks>
        void SwitchOffBroken(AutomationSystem system)
        {
            var instances = system.Uses;
            var sequences = system.Sequences;

            for (var n = 0; n < instances.Count && system.Enabled; n++)
            {
                if (!instances[n].Enabled)
                    continue;

                var faulted = false;
                for (var q = 0; q < sequences.Count && !faulted; q++)
                    faulted = Parks(sequences[q], instances[n]);

                var triggers = system.Triggers;
                for (var t = 0; t < triggers.Count && !faulted; t++)
                {
                    var alone = Alone(system, triggers[t]);
                    faulted = alone != null && Parks(alone, instances[n]);
                }
            }
        }

        /// <summary>Switches off what <paramref name="sequence" /> runs on where the program says it cannot work there, and answers whether it did.</summary>
        bool Parks(Sequence sequence, SystemInstance instance)
        {
            var runner = Get(sequence, instance);
            if (runner == null || !runner.Faults.Found.Parks)
                return false;

            Faulted(sequence, instance, runner.Faults.Found);
            return true;
        }

        /// <summary>
        /// Keeps one poller for each <c>when</c> and <c>every</c> of each system on each of its instances, and one runner for
        /// each trigger that acts itself, and retires the rest.
        /// </summary>
        /// <remarks>Done with the check, when the program or its wiring has changed, and on no other tick: triggers come and go only by those.</remarks>
        void Match()
        {
            _watches.Begin();
            _runs.Begin();

            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                var instances = system.Uses;
                var triggers = system.Triggers;

                for (var t = 0; t < triggers.Count; t++)
                {
                    var trigger = triggers[t];
                    _runs.KeepTrigger(trigger);
                    var signal = trigger as SignalTrigger;
                    var watched = trigger is WhenTrigger || trigger is EveryTrigger || (signal != null && signal.IsWatched);
                    var alone = _runs.Alone(system, trigger);

                    for (var n = 0; n < instances.Count; n++)
                    {
                        if (watched)
                            _watches.Keep(system, t, trigger, instances[n]);

                        if (alone != null)
                            _runs.Keep(system, t, alone, instances[n]);
                    }
                }
            }

            _watches.Sweep();
            _runs.Sweep();
        }

        /// <summary>Switches off what <paramref name="sequence" /> runs on, with why, where it is switched on.</summary>
        /// <remarks>
        /// A logic system of its own as a whole, and one of a blueprint's alone, so the port airlock missing a door leaves
        /// the front one working. Never switched back on here: starting a machine again is somebody's decision.
        /// </remarks>
        public void Faulted(Sequence sequence, SystemInstance instance, SequenceFault fault)
        {
            var system = sequence.System;
            if (system == null || !system.Enabled)
                return;

            // A slot or an option is named as it is; a block by the name it goes by.
            var block = fault.Which == null || fault.Problem == SequenceProblem.EmptySlot
                || fault.Problem == SequenceProblem.Unfilled || fault.Problem == SequenceProblem.Mistyped
                ? fault.Which
                : _automation.Called(fault.Which, _world.Values);
            var halt = new Halt(fault.Problem, sequence.Number, fault.Step, fault.Because, block);

            if (system.IsBlueprint && instance != null)
            {
                if (!instance.Enabled)
                    return;

                instance.Enabled = false;
                instance.Halt = halt;
                Revise();
                return;
            }

            system.Enabled = false;
            system.Halt = halt;
            Revise();
        }

        /// <summary>Looks at the construct for each sequence, on every system and every blockset of a blueprint, for an open editor to show what is gone or may have been swapped.</summary>
        /// <remarks>
        /// Only for somebody looking, and nothing is switched off here: a run finds the same at the step that needs it. Asked
        /// as a screen opens, and while one is open on the tick the names are worked out again or the program is checked,
        /// never on a tick of its own; it looks only where what the names point at, or the program, has changed since the
        /// last look. What a block being waited on says about
        /// finishing is the run's own to read, on its own looks, so an open editor leaves every run's timing as it was.
        /// </remarks>
        public void Look()
        {
            var bindings = Bindings;
            if (bindings.Revision == _lookedBindings && _automation.Edits == _lookedEdits && Reconciled == _lookedRunners)
                return;

            Looks++;
            Revise();
            _lookedBindings = bindings.Revision;
            _lookedEdits = _automation.Edits;
            _lookedRunners = Reconciled;

            _runs.Look(bindings);
        }
    }
}
