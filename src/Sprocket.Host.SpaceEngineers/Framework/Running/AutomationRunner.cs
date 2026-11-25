using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// Runs everything, one tick at a time.
    /// </summary>
    /// <remarks>Always live: an edit takes effect on the next tick.</remarks>
    public sealed class AutomationRunner
    {
        // How deep sets of states may start one another within one step: a when that sets a state another when watches,
        // and so on. Past it the set is made and starts nothing more, so two whens that set each other's state cannot
        // go on for ever within a tick.
        const int DeepestSets = 16;

        readonly Runners _runs;
        readonly StateValues _states;
        readonly Pollers _watches;
        readonly Automation _automation;
        readonly GridWorld _world;
        readonly Bindings _bindings = new Bindings();
        readonly Rota _rota;

        readonly bool _runsRota;

        bool _reshaped = true;
        int _told = int.MinValue;
        int _setting;

        // Each when on the state a set writes, and whether it held before the set: a pair of lists for each depth of sets
        // that start one another, made once and cleared, so a set allocates nothing.
        readonly List<StateTrigger>[] _watching = new List<StateTrigger>[DeepestSets + 1];
        readonly List<bool>[] _held = new List<bool>[DeepestSets + 1];
        int _checkedEdits = -1;
        int _checkedWiring = -1;
        int _checkedUnstarted = -1;
        int _checkedStarts = -1;
        int _checkedNamespaces = -1;
        int _matchedSwitches = -1;
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

        /// <summary>A runner on <paramref name="rota" />, the world's rota that every runner in it shares.</summary>
        public AutomationRunner(Automation automation, GridWorld world, Rota rota)
        {
            _automation = automation;
            _world = world;
            _rota = rota;
            Forced = new ForcedActs(rota, () => FastestUpdate);
            _runs = new Runners(this, automation);
            _watches = new Pollers(this, automation);
            _states = new StateValues(automation);
            Live = true;
            Clock = () => CurrentTick;

            _automation.Reshaped += () => _reshaped = true;
        }

        /// <summary>
        /// The game's current frame: the later of the tick its rota is running and the tick it was last told.
        /// </summary>
        /// <remarks>
        /// A runner on the world's rota reads the rota's clock; a copy, whose rota nothing runs, is told the tick by whatever
        /// ticks it.
        /// </remarks>
        public int CurrentTick => Math.Max(_told, _rota.Now);

        /// <summary>Sprocket's clock as the readers of blocks are given it, which tells them the tick they read on.</summary>
        public Func<int> Clock { get; }

        /// <summary>
        /// What the program's declarations currently point at.
        /// </summary>
        public Bindings Bindings
        {
            get
            {
                // Checked on every read as well as on a tick, so a block the editor just declared reads back found. Two
                // integer comparisons where nothing moved.
                Refreshed();
                return _bindings;
            }
        }

        /// <summary>
        /// Whether the machine Sprocket runs on can run anything: its block powered and switched on.
        /// </summary>
        /// <remarks>Set by whatever owns the block, as whether it can run changes; true by default.</remarks>
        public bool Live { get; set; }

        /// <summary>The fewest ticks between two runs of anything on a repeat, whatever the program asks for: a timer, a <c>when</c>'s look, a run's look at a step it waits on.</summary>
        /// <remarks>
        /// Set by whatever owns the runner, from the world's fastest update; 1 by default. A change the game announces and a
        /// timed wait keep their own moment.
        /// </remarks>
        public int FastestUpdate { get; set; } = 1;

        /// <summary>The blocks being kept at what they were told by force.</summary>
        public ForcedActs Forced { get; }

        /// <summary>Where every run of this program writes what it does, or null, which is what the world's debug log being off means.</summary>
        /// <remarks>Set by whatever owns the runner, from the world's settings. Each place a run would write a line costs it one null check while this is null.</remarks>
        public RunLog Log { get; set; }

        /// <summary>Whether a screen is open on this construct, which is who <see cref="Look" /> is for.</summary>
        /// <remarks>Set by the screen as it opens and closes.</remarks>
        public bool Looking { get; set; }

        /// <summary>Advances everything to the given frame.</summary>
        /// <remarks>For a runner whose rota nothing else runs; one on the world's rota is brought up to date by <see cref="Prepare" />.</remarks>
        public void Tick(int tick)
        {
            _told = tick;

            // Two integer comparisons on an ordinary tick. See Bindings.Refresh.
            Refreshed();

            Checking();

            // The rota calls each sequence that is due. A world's rota is run by the session once every Sprocket in it has
            // ticked, so every runner's bindings are current when its sequences are called.
            if (_runsRota)
                _rota.Run(tick);
        }

        /// <summary>Starts the clock at <paramref name="tick" />, which is where a world just loaded stands before its first tick.</summary>
        public void Begin(int tick)
        {
            _told = tick;
            _rota.Begin(tick);
        }

        /// <summary>Makes the runners and checks the program now, rather than on the next tick, for a press that arrives first.</summary>
        public void Prepare()
        {
            Refreshed();
            Checking();
        }

        /// <summary>Lets go of every run, watch and state value it holds, so each is made again on the next tick from the construct's memory as it stands.</summary>
        /// <remarks>For a copy whose memory was put back in place from the one that lent it, before the program is wired again.</remarks>
        public void Forget()
        {
            _runs.Forget();
            _watches.Forget();
            _states.Forget();
            _reshaped = true;
        }

        /// <summary>Takes everything this program put on the rota off it, when its Sprocket goes away.</summary>
        public void Retire()
        {
            Forced.Retire();
            _runs.Retire();
            _watches.Retire();
        }

        /// <summary>How many times the program has been checked.</summary>
        public int Checks { get; private set; }

        /// <summary>How many times an open editor's look has looked at the construct.</summary>
        public int Looks { get; private set; }

        /// <summary>How many times the runners have been matched to the program.</summary>
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
        public SequenceRunner Get(Sequence sequence)
        {
            var system = sequence.System;
            var first = system != null && system.Uses.Count > 0
                ? system.Uses[0].Number
                : 0;

            return _runs.Get(Runners.Key(sequence.Id, first));
        }

        /// <summary>Live state for a sequence on one particular one of its system.</summary>
        public SequenceRunner Get(Sequence sequence, SystemInstance instance)
        {
            return _runs.Get(Runners.Key(sequence.Id, instance?.Number ?? 0));
        }

        /// <summary>The runners of the program.</summary>
        public Runners Runs => _runs;

        /// <summary>What each installation holds in its system's states.</summary>
        public StateValues States => _states;

        /// <summary>
        /// Does what <paramref name="set" /> of <paramref name="system" /> says on <paramref name="instance" />, and starts at
        /// once each <c>when</c> on that state whose test came to hold by it.
        /// </summary>
        /// <remarks>
        /// A set that leaves the state as it was starts nothing. Within one step, sets started by sets go only so deep; past
        /// that the value is still set, and nothing more is started.
        /// </remarks>
        public void Set(AutomationSystem system, SystemInstance instance, SetState set)
        {
            Set(system, instance, set, null);
        }

        /// <summary>Does what <paramref name="set" /> says, taking <paramref name="worked" /> as the value where it is not null: what an expression of the step came to.</summary>
        public void Set(AutomationSystem system, SystemInstance instance, SetState set, Literal? worked)
        {
            var state = system.FindState(set.State);
            if (state == null)
                return;

            var from = set.From.Length == 0 ? null : system.FindState(set.From);
            if (set.From.Length > 0 && from == null)
                return;

            // A setting with no value in the installation sets nothing; the sequence's check says so before it runs.
            var value = from == null ? worked ?? set.Value : _states.Read(system, instance, from);
            if (value.IsSetting && (instance == null || !instance.Gives(value.Text, out value)))
                return;

            // What held before the set, of each when on the state, so only what came to hold is started.
            var watching = _watching[_setting] ?? (_watching[_setting] = new List<StateTrigger>());
            var held = _held[_setting] ?? (_held[_setting] = new List<bool>());
            watching.Clear();
            held.Clear();
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var watched = triggers[i] as StateTrigger;
                if (watched == null || watched.Test.State != state.Name && watched.Test.Other != state.Name)
                    continue;

                watching.Add(watched);
                held.Add(_states.Holds(system, instance, watched.Test));
            }

            if (!_states.Write(system, instance, state, value) || _setting >= DeepestSets)
                return;

            _setting++;
            for (var i = 0; i < watching.Count; i++)
            {
                if (!held[i] && _states.Holds(system, instance, watching[i].Test))
                    Fire(system, watching[i], instance);
            }

            // A when whose expression reads the state looks again now, as a when on the state alone is started now.
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i] is ExpressionTrigger)
                    _watches.Poller(triggers[i], instance)?.Set(state.Name);
            }

            _setting--;
        }

        /// <summary>The pollers of the program.</summary>
        public Pollers Watches => _watches;

        /// <summary>Says something an open screen shows of the runs has changed, on the tick it changed.</summary>
        public void Revise()
        {
            Revision++;
        }

        /// <summary>Does what <paramref name="trigger" /> of <paramref name="system" /> does on <paramref name="instance" />, because what it watches for came round.</summary>
        /// <remarks>
        /// A sequence switched off lets it go by, one already running counts it as skipped, and a stop does nothing where the
        /// sequence is not running.
        /// </remarks>
        public void Fire(AutomationSystem system, Trigger trigger, SystemInstance instance)
        {
            Log?.Fired(system, trigger, instance, CurrentTick);

            var run = trigger.Does;
            if (run == null)
                return;

            var sequence = system.FindSequence(run.Number);
            if (sequence == null)
                return;

            if (run.Stops)
            {
                Halt(sequence, instance, StopCause.Trigger, 0);
                return;
            }

            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Triggered(_bindings, CurrentTick);
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
        public void Call(Sequence sequence, SystemInstance instance, bool byHand = false)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Call(_bindings, CurrentTick, Live, byHand);
        }

        /// <summary>
        /// Runs or stops what waits on the moment <paramref name="key" /> a mod raised on the block <paramref name="entityId" />,
        /// once for this raise, as a press of a button wired to it would.
        /// </summary>
        /// <remarks>Asked only as a mod raises, never on a tick; walks the systems' signal triggers.</remarks>
        public void Raised(long entityId, MemberKey key)
        {
            RaisesWalked++;
            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                if (!system.Enabled)
                    continue;

                var triggers = system.Triggers;
                for (var t = 0; t < triggers.Count; t++)
                {
                    var signal = triggers[t] as SignalTrigger;
                    // A bare signal names the one member of its id in any namespace; more than one is the summary's fault, below.
                    if (signal == null || !signal.IsWatched || !signal.Key.Names(key))
                        continue;

                    var sequence = system.FindSequence(signal.Runs);
                    if (sequence == null)
                        continue;

                    var instances = system.Uses;
                    for (var n = 0; n < instances.Count; n++)
                    {
                        // A faulted or waiting trigger starts nothing, as on the watched path.
                        if (instances[n].Enabled && !SequenceSummary.Waits(system, signal, instances[n]) && From(signal, instances[n], entityId))
                        {
                            if (signal.Stops)
                                Halt(sequence, instances[n], StopCause.Trigger, 0);
                            else
                                Call(sequence, instances[n]);
                        }
                    }
                }
            }
        }

        /// <summary>How many raises have walked the triggers, which a test counts.</summary>
        public int RaisesWalked { get; private set; }

        /// <summary>Whether <paramref name="signal" />, on <paramref name="instance" />, waits on a moment a mod raises rather than a change watched for.</summary>
        bool Raises(SignalTrigger signal, SystemInstance instance)
        {
            if (signal == null)
                return false;

            foreach (var source in SystemInstance.Through(instance, signal.Sources))
            {
                var block = _bindings.Entity(source);
                var raised = block?.Kind.FindEvent(signal.Key);
                if (raised != null && raised.IsRaised)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="signal" />, on <paramref name="instance" />, listens to the block <paramref name="entityId" />.</summary>
        bool From(SignalTrigger signal, SystemInstance instance, long entityId)
        {
            foreach (var source in SystemInstance.Through(instance, signal.Sources))
            {
                var block = _bindings.Entity(source);
                if (block != null && block.Block.EntityId == entityId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Abandons a run, as a button that stops a sequence does.
        /// </summary>
        /// <remarks>
        /// What was started stays started: a door told to open goes on opening. Stopping a sequence that is not running does
        /// nothing.
        /// </remarks>
        public void Halt(Sequence sequence, SystemInstance instance, StopCause by, int from)
        {
            var runner = Get(sequence, instance);
            if (runner != null)
                runner.Interrupt(by, from);
        }

        /// <summary>
        /// Keeps the set of runners matching the sequences and the ones of each system.
        /// </summary>
        /// <remarks>Done on the tick after the program says it gained or lost something, and on no other.</remarks>
        void Reconcile()
        {
            Reconciled++;
            Revise();
            _runs.Reconcile();
        }

        /// <summary>Matches the runners to the program where it gained or lost something, checks it where it or its wiring changed, and matches what is watched where a switch was flipped; nothing otherwise.</summary>
        /// <remarks>
        /// Asked as the program is brought up to date with a change, and by an editor about to show what is wrong; a flag and
        /// six integer comparisons. The host gaining or losing a namespace is a change to the program's wiring, so what is
        /// in that namespace is checked and watched as if the program were bound with the host as it now is.
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

            if (_automation.Edits != _checkedEdits || _automation.Wiring != _checkedWiring || _world.Unstartable.Told != _checkedUnstarted
                || _automation.Starts != _checkedStarts || HostNamespaces.Changes != _checkedNamespaces)
            {
                Check();
            }
            else if (_automation.Switches != _matchedSwitches)
            {
                // A switch alone changes what is watched, never what is wrong, so what a run met stands.
                Match();
            }
        }

        /// <summary>Gives <paramref name="runner" /> what the check <paramref name="found" />, forgetting all it knew where <paramref name="afresh" />.</summary>
        static void Checked(SequenceRunner runner, SequenceFault found, bool afresh)
        {
            if (afresh)
                runner.Faults.Afresh(found);
            else
                runner.Faults.Checked(found);
        }

        /// <summary>What the program says is wrong with each sequence, on every system and every blockset of a blueprint, with what cannot work switched off.</summary>
        /// <remarks>
        /// On the tick after the program or its wiring changes, and on no other. After something was switched on, every problem
        /// is forgotten first, so that what is still wrong is found again.
        /// </remarks>
        void Check()
        {
            Checks++;
            var afresh = _checkedStarts >= 0 && _automation.Starts != _checkedStarts;
            _checkedEdits = _automation.Edits;
            _checkedWiring = _automation.Wiring;
            _checkedUnstarted = _world.Unstartable.Told;
            _checkedStarts = _automation.Starts;
            _checkedNamespaces = HostNamespaces.Changes;

            Match();

            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                var instances = system.Uses;
                var sequences = system.Sequences;

                for (var n = 0; n < instances.Count; n++)
                {
                    for (var q = 0; q < sequences.Count; q++)
                    {
                        var runner = Get(sequences[q], instances[n]);
                        if (runner != null)
                            Checked(runner, SequenceSummary.Checked(_world, Bindings, sequences[q], instances[n]), afresh);
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
        /// <remarks>Faults only: a sequence nothing starts yet is left switched on.</remarks>
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
        /// Keeps one poller for each <c>when</c> and <c>every</c> of each system on each of its instances that may run and retires the rest; and stops each run of what is switched off.
        /// </summary>
        /// <remarks>Done with the check, when the program or its wiring has changed, and alone when a switch is flipped.</remarks>
        void Match()
        {
            _matchedSwitches = _automation.Switches;
            _watches.Begin();

            var systems = _automation.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var system = systems[s];
                var instances = system.Uses;
                var triggers = system.Triggers;

                for (var t = 0; t < triggers.Count; t++)
                {
                    var trigger = triggers[t];
                    var signal = trigger as SignalTrigger;
                    var watched = trigger is WhenTrigger || trigger is EveryTrigger || trigger is ExpressionTrigger || (signal != null && signal.IsWatched);

                    for (var n = 0; n < instances.Count; n++)
                    {
                        // Switched off, or waiting to be filled in, it has nothing watched, so it costs no look until it may run.
                        // A moment a mod raises starts its sequence as it is raised, so it is never watched.
                        if (watched && system.Enabled && instances[n].Enabled && !SequenceSummary.Waits(system, trigger, instances[n])
                            && !Raises(signal, instances[n]))
                            _watches.Keep(system, t, trigger, instances[n]);
                    }
                }
            }

            _watches.Sweep();

            // A run of what was just switched off stops on the tick of the switch.
            _runs.Switched();
        }

        /// <summary>Switches off what <paramref name="sequence" /> runs on, with why, where it is switched on.</summary>
        /// <remarks>
        /// A logic system of its own as a whole, and one installation of a blueprint alone. Never switched back on here. A
        /// fault that waits for a slot to be filled switches nothing off.
        /// </remarks>
        public void Faulted(Sequence sequence, SystemInstance instance, SequenceFault fault)
        {
            var system = sequence.System;
            if (system == null || !system.Enabled || fault.Waits)
                return;

            // A slot or an option is named as it is; a block by the name it goes by.
            var block = fault.Which == null || fault.Problem == SequenceProblem.EmptySlot
                || fault.Problem == SequenceProblem.Unfilled || fault.Problem == SequenceProblem.Mistyped || fault.Problem == SequenceProblem.Ambiguous
                || fault.Problem == SequenceProblem.AbortedProblem
                ? fault.Which
                : _automation.Called(fault.Which, _world.Values);
            var halt = new Halt(fault.Problem, sequence.Number, fault.Step, fault.Because, block);

            if (system.IsBlueprint && instance != null && !fault.IsAboutSystem)
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
        /// Asked as a screen opens, and while one is open on the tick the names are worked out again or the program is
        /// checked, never on a tick of its own. Switches nothing off and leaves every run's timing as it was.
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
