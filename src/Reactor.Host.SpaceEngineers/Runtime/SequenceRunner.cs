using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Runs one sequence, and carries everything the editor needs to show what it is
    /// doing right now.
    /// </summary>
    public sealed class SequenceRunner
    {
        readonly Sequence _sequence;

        int _stepIndex = -1;
        int _waitTicks;

        public SequenceRunner(Sequence sequence) : this(sequence, null)
        {
        }

        /// <summary>
        /// One sequence, running for one of a system.
        /// </summary>
        /// <remarks>
        /// The same sequence runs once for each of a system on the ship, each resolving the
        /// block slots the author built with into its own doors. Null means the slots are the
        /// blocks themselves, which is what the first one of a system does and what every
        /// system built before block slots existed keeps doing.
        /// </remarks>
        public SequenceRunner(Sequence sequence, SystemInstance instance)
        {
            _sequence = sequence;
            _instance = instance;
            LastFiredTick = -1;
            LastSkippedTick = -1;
        }

        /// <summary>
        /// Starts another sequence of the same system, on the same one of it.
        /// </summary>
        /// <remarks>
        /// Set by whatever owns the runners, because a sequence knows nothing about anything running
        /// beside it. Null until then, which is what a sequence run on its own in a test has.
        /// </remarks>
        public Action<int> Calls { get; set; }

        readonly SystemInstance _instance;

        /// <summary>
        /// Whether this sequence may run for this one of its system.
        /// </summary>
        /// <remarks>
        /// Three switches, all of which have to be on: the sequence's, its system's, and this
        /// one of the system's. They are asked separately everywhere else because they are
        /// three different intentions - "I parked this rule", "I am rewiring the whole thing",
        /// "leave the port airlock alone while I weld" - and none may overwrite another.
        /// </remarks>
        bool CanRun => _sequence.CanRun && (_instance == null || _instance.Enabled);

        /// <summary>
        /// Scratch for turning the system's parts into this one's blocks.
        /// </summary>
        /// <remarks>
        /// Two, because the trigger is sampled on the same tick a step is acting and one
        /// buffer would have them overwrite each other. Reused rather than allocated, because
        /// this happens for every running sequence on every tick and core sits inside the
        /// game's update loop.
        /// </remarks>
        /// <summary>
        /// What the program's names point at, for the length of this tick.
        /// </summary>
        /// <remarks>
        /// Held rather than passed down, because every private step of a tick wants it and threading
        /// it through eight signatures says nothing the field does not. Set by the two public entry
        /// points, so nothing below them can read one from a previous tick.
        /// </remarks>
        Bindings _bindings;

        readonly List<string> _actedOn = new List<string>();
        readonly List<string> _watched = new List<string>();

        List<string> ActedOn(IList<string> slots)
        {
            _actedOn.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < slots.Count; i++)
                    _actedOn.Add(slots[i]);
            }
            else
            {
                _instance.Resolve(slots, _actedOn);
            }
            return _actedOn;
        }

        List<string> Watched(IList<string> slots)
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
            return _watched;
        }

        public Sequence Sequence => _sequence;

        public bool IsRunning { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep => _stepIndex;

        /// <summary>True while the sequence is held at an await or a wait.</summary>
        public bool IsWaiting { get; private set; }

        /// <summary>How much of a wait is left to spend, in ticks.</summary>
        /// <remarks>Written down when a world is saved, so a pause survives being reloaded.</remarks>
        public int WaitTicks
        {
            get { return _waitTicks; }
        }

        public int LastFiredTick { get; private set; }

        public int FiredCount { get; private set; }

        /// <summary>
        /// How often the trigger fired while the sequence was already running.
        /// </summary>
        /// <remarks>
        /// Re-triggering skips rather than queueing, restarting, or running a second
        /// copy. Skipping is the only option that cannot silently accumulate work,
        /// and it matches how people think about a system that is busy. With real
        /// gates the wait is legible - "waiting for the inner door to shut" - so busy
        /// explains itself instead of being a mystery.
        /// </remarks>
        public int SkippedCount { get; private set; }

        public int LastSkippedTick { get; private set; }

        /// <summary>
        /// How often something called this sequence while it was switched off.
        /// </summary>
        /// <remarks>
        /// Counted separately from being busy because it is a different piece of news, and counted at
        /// all because the alternative is silence: a step saying <c>run</c> that meets a parked sequence
        /// would otherwise evaporate, which is the one thing Reactor does nowhere else.
        /// <para>
        /// Calls only. Its own trigger firing while it is off is not news - that is what off means, and
        /// counting it would report a door somebody opened ten times as ten refusals.
        /// </para>
        /// </remarks>
        public int RefusedCount { get; private set; }

        /// <summary>Whether the watched signal is true right now.</summary>
        public bool TriggerSignalIsTrue { get; private set; }

        /// <summary>One or more listened-to entities are no longer on the construct.</summary>
        public bool HasMissingSources { get; private set; }

        /// <summary>
        /// One tick of this sequence.
        /// </summary>
        /// <remarks>
        /// <paramref name="live" /> is whether the machine Reactor runs on can do anything at all:
        /// its own block powered and switched on. Nothing starts and nothing moves on while it is
        /// false, and what was already running is <b>kept</b> rather than stopped - a power cut is
        /// not somebody pressing a switch, and coming back up part way through a cycle is the whole
        /// point of remembering the run at all.
        /// <para>
        /// The trigger is still sampled. Edge detection is stateful, so a skipped sample makes the
        /// next one wrong, and a door opened during a blackout must not read as having just opened
        /// when the lights come back.
        /// </para>
        /// <para>
        /// The same rule the programmable block follows: it will not instantiate a script until its
        /// block is powered and enabled, and it asks for updates again when power arrives. On a world
        /// still loading, that deferral is also what keeps a resumed run from judging a half-built
        /// construct.
        /// </para>
        /// </remarks>
        public void Tick(GridWorld world, Bindings bindings, int tick, bool live = true)
        {
            _bindings = bindings;

            // Sample every tick, even while running or switched off. Edge detection
            // is stateful, so a skipped sample makes the next one wrong.
            var triggered = SampleTrigger(world);

            if (!live)
                return;

            // A run somebody started by hand is theirs, and the switch is not about them: they are
            // standing at the screen watching it. Everything else a switched-off sequence refuses.
            if (!CanRun && !_byHand)
            {
                // Only when there is a run to abandon. This is the tick path, and a parked sequence
                // sits here for as long as it stays parked: stopping one that is not running costs
                // the same as stopping one that is, and nothing outside can tell either way.
                if (IsRunning)
                    Stop();

                return;
            }

            if (IsRunning)
            {
                if (triggered)
                {
                    SkippedCount++;
                    LastSkippedTick = tick;
                }
                Advance(world);
                return;
            }

            if (triggered)
                Start(world, tick);
        }

        /// <summary>Starts the sequence by name, from another sequence, a trigger or a button.</summary>
        /// <remarks>
        /// The switch holds here. A parked sequence is parked for everything, including being called by
        /// another one - what a person switched off must not run because something else asked.
        /// <para>
        /// So is the machine's own power. A press arrives as a call rather than as a tick, so a slot
        /// on somebody's panel would otherwise start a cycle on a Reactor that has no power at all.
        /// </para>
        /// <para>
        /// Either way the call is refused rather than dropped. Off is a legitimate answer and silence
        /// is not: the refusal is counted and shown, the way a switched-off block refuses an action
        /// whoever asks.
        /// </para>
        /// </remarks>
        public void Call(GridWorld world, Bindings bindings, int tick, bool live = true,
            bool byHand = false)
        {
            _bindings = bindings;

            // Power is refused whoever asks: a machine with none does nothing, and a person at the
            // screen of a dead block is not an exception to that.
            if (!live)
            {
                RefusedCount++;
                return;
            }

            // The switch is not. Somebody who parks a sequence so its triggers leave it alone, and
            // then presses run, has said exactly what they want - and that is the state anybody
            // debugging one is in.
            if (!CanRun && !byHand)
            {
                RefusedCount++;
                return;
            }

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                return;
            }

            _byHand = byHand;
            Start(world, tick);
        }

        /// <summary>
        /// Whether the run going on now was asked for by a person rather than by a trigger.
        /// </summary>
        /// <remarks>
        /// It decides one thing: that the switch does not stop it on the next tick. Cleared when the
        /// run ends however it ends, so the next one is judged on its own.
        /// </remarks>
        bool _byHand;

        /// <summary>
        /// Puts a run back where a reload interrupted it.
        /// </summary>
        /// <remarks>
        /// A world saved mid-cycle has to carry on when it is loaded: the doors are where they were
        /// and the room is as empty as it was, so forgetting the run would leave an airlock with its
        /// inner door shut, its outer door closed, and nobody left to open either.
        /// <para>
        /// The step and the clock are restored; what was started is not. Whatever was invoked before
        /// the save has either arrived by now or has not, and an await asks the world rather than
        /// anything written down - so the acts between the previous await and this step are put back
        /// as what it is waiting on, and the answer comes from the doors themselves.
        /// </para>
        /// <para>
        /// Nothing is restored where the sequence cannot run or the step is no longer there. Both
        /// happen when a program is edited between saving and loading, and a run resumed into a
        /// sequence that has since changed is a machine doing something nobody asked for.
        /// </para>
        /// </remarks>
        public void Resume(int step, bool waiting, int waitTicks)
        {
            if (!CanRun || step < 0 || step >= _sequence.Steps.Count)
                return;

            IsRunning = true;
            IsWaiting = waiting;
            _stepIndex = step;
            _waitTicks = waitTicks < 0 ? 0 : waitTicks;

            _outstanding.Clear();

            // Backwards to the await before this step, because that is the span an await covers. An
            // act that has already arrived is no burden: Settled says so at once.
            for (var i = step - 1; i >= 0; i--)
            {
                var earlier = _sequence.Steps[i];
                if (earlier is Await)
                    break;

                var act = earlier as Act;
                if (act != null)
                    _outstanding.Add(act);
            }
        }

        public void Stop()
        {
            IsRunning = false;
            IsWaiting = false;
            _byHand = false;
            _stepIndex = -1;
            _waitTicks = 0;
            _outstanding.Clear();
        }

        /// <summary>
        /// Whether every entity this step acted on has reached the state the action
        /// was reaching for.
        /// </summary>
        /// <remarks>
        /// An action that declares no destination - switching a light on - is done
        /// the instant it is invoked, so instant things stay instant and only things
        /// that genuinely take time are waited on.
        ///
        /// An entity that never gets there holds the sequence indefinitely. That is
        /// the right failure: carrying on regardless is how a jammed door vents a
        /// ship. It does mean being visibly stuck has to be impossible to miss, which
        /// is why the step being waited on is reported rather than inferred.
        /// </remarks>
        public bool Settled(GridWorld world, Act step)
        {
            if (!step.HasAction)
                return true;

            var settling = ActedOn(step.Targets);
            for (var i = 0; i < settling.Count; i++)
            {
                var entity = _bindings.Entity(settling[i]);
                if (entity == null)
                    continue;

                var action = entity.Kind.FindAction(step.ActionId);
                if (action == null || string.IsNullOrEmpty(action.LeadsToStateId))
                    continue;

                var state = entity.Kind.FindState(action.LeadsToStateId);
                if (state == null)
                    continue;

                if (!state.Holds(world.Values, entity.GetValue(state.PropertyId)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether a signal reads as true right now.
        /// </summary>
        /// <remarks>
        /// A value does not carry its own kind, so reading one needs the world that made it. An event
        /// is declared against a property whose reading is a flag in every case that matters, and
        /// anything else answers the same question - is there anything there.
        /// </remarks>
        static bool Reads(GridWorld world, GridEntity entity, string propertyId)
        {
            var property = entity.Kind.FindProperty(propertyId);
            var kind = property == null ? ValueKind.Flag : property.Kind;

            return world.Values.Flag(kind, entity.GetValue(propertyId));
        }

        void Start(GridWorld world, int tick)
        {
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;
            _stepIndex = 0;
            EnterStep();
            Advance(world);
        }

        void EnterStep()
        {
            _waitTicks = 0;
            if (_stepIndex < 0 || _stepIndex >= _sequence.Steps.Count)
                return;

            var wait = _sequence.Steps[_stepIndex] as Wait;
            if (wait != null)
                _waitTicks = wait.Ticks;
        }

        /// <summary>
        /// Walks the steps until something holds the sequence up.
        /// </summary>
        /// <remarks>
        /// An act starts and the next line follows in the same tick, so several acts overlap by
        /// default. <see cref="Await" /> is what stops that, and it is what makes a numbered list
        /// mean what it looks like it means.
        /// </remarks>
        void Advance(GridWorld world)
        {
            while (_stepIndex >= 0 && _stepIndex < _sequence.Steps.Count)
            {
                var step = _sequence.Steps[_stepIndex];

                var act = step as Act;
                if (act != null)
                {
                    IsWaiting = false;
                    Execute(world, act);

                    // Kept so an await below knows what it is waiting for. Nothing with a
                    // destination is nothing to wait on, which is how an instant action stays
                    // instant.
                    if (!Settled(world, act))
                        _outstanding.Add(act);

                    Step();
                    continue;
                }

                var run = step as Run;
                if (run != null)
                {
                    IsWaiting = false;

                    // Started and left to it. An await waits for actions this sequence began, not for
                    // a sequence somebody called - waiting for one of those is the concurrency work.
                    if (Calls != null)
                        Calls(run.Number);

                    Step();
                    continue;
                }

                if (step is Await)
                {
                    // Nothing to ask is not nothing to wait for. The half of the job that was
                    // asked for has not happened, and walking on would act on the rest of the ship
                    // as though it had - which for an airlock means opening the outer door because
                    // the inner one is no longer there to be shut.
                    //
                    // Holding is all this does. A missing block is a fault, and a sequence at fault
                    // is switched off within the second by ReactorEditor.ParkBroken, which is what
                    // stops the run and marks it. The same road a step that cannot act at all
                    // already travels.
                    if (Missing() || !Finished(world))
                    {
                        IsWaiting = true;
                        return;
                    }

                    _outstanding.Clear();
                    IsWaiting = false;
                    Step();
                    continue;
                }

                if (_waitTicks > 0)
                {
                    _waitTicks--;
                    IsWaiting = true;
                    return;
                }

                IsWaiting = false;
                Step();
            }

            Stop();
        }

        void Step()
        {
            _stepIndex++;
            EnterStep();
        }

        /// <summary>
        /// Whether anything still being waited for names a block that is not there.
        /// </summary>
        /// <remarks>
        /// Unbound rather than unsettled: the question is not whether the door has shut, it is
        /// whether there is still a door. A block that has not loaded yet counts too, and holding is
        /// the right answer for both - one of them arrives, and the other gets the sequence switched
        /// off for being at fault.
        /// </remarks>
        bool Missing()
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!_outstanding[i].HasAction)
                    continue;

                var waiting = ActedOn(_outstanding[i].Targets);
                for (var q = 0; q < waiting.Count; q++)
                {
                    if (_bindings.Entity(waiting[q]) == null)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Whether everything started since the last await has got where it was going.</summary>
        bool Finished(GridWorld world)
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!Settled(world, _outstanding[i]))
                    return false;
            }

            return true;
        }

        /// <summary>What has been started and not yet waited for.</summary>
        readonly List<Act> _outstanding = new List<Act>();

        void Execute(GridWorld world, Act step)
        {
            if (!step.HasAction)
                return;

            var acting = ActedOn(step.Targets);
            for (var i = 0; i < acting.Count; i++)
            {
                var entity = _bindings.Entity(acting[i]);
                if (entity == null)
                    continue;

                if (entity.Kind.FindAction(step.ActionId) == null)
                    continue;

                world.Invoke(entity, step.ActionId);
            }
        }

        /// <summary>
        /// Samples everything that starts this sequence, and answers whether any of it fired.
        /// </summary>
        /// <remarks>
        /// All of them, every tick, because edge detection is stateful: a trigger that is not sampled
        /// has no previous reading, and the next sample it gets is wrong. Two triggers on one sequence
        /// are the ordinary case - the inside button and the outside one - and each keeps its own
        /// reading, so one firing never robs the other of its edge.
        /// </remarks>
        bool SampleTrigger(GridWorld world)
        {
            var anySignal = false;
            var anyMissing = false;
            var fired = false;
            var watched = 0;

            var system = _sequence.System;
            var triggers = system == null ? null : system.Triggers;
            for (var i = 0; triggers != null && i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger.Runs != _sequence.Number)
                    continue;

                Watch watch = Watching(trigger);
                if (Sampled(world, trigger, watch))
                    fired = true;

                // What the sample just read is this trigger's, until the next one overwrites it.
                watch.Live = TriggerSignalIsTrue;

                anySignal = anySignal || TriggerSignalIsTrue;
                anyMissing = anyMissing || HasMissingSources;
                watched++;
            }

            // A trigger somebody has changed or deleted is still in here holding an edge nothing
            // will ever read again. Swept only when there is something to sweep, which is after an
            // edit and never on an ordinary tick.
            if (_watching.Count > watched)
                Forget(triggers);

            TriggerSignalIsTrue = anySignal;
            HasMissingSources = anyMissing;
            return fired;
        }

        bool Sampled(GridWorld world, Trigger trigger, Watch watch)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
                return when.RequiresAll ? SampleWhenAll(world, when, watch) : SampleWhen(world, when, watch);

            var every = trigger as EveryTrigger;
            if (every != null)
                return SampleEvery(every, watch);

            // A trigger the construct answers fires by being fired: a slot invokes Reactor's action
            // and the host calls in. There is nothing here to sample.
            return false;
        }

        /// <summary>
        /// What one trigger read last time, kept per trigger.
        /// </summary>
        /// <remarks>
        /// Per trigger rather than per sequence, because an edge is a change since <em>this</em>
        /// trigger last looked. Shared state made the second trigger on a sequence read the first
        /// one's history, which fires nothing or fires twice.
        /// </remarks>
        sealed class Watch
        {
            public readonly Dictionary<string, bool> Signals = new Dictionary<string, bool>();

            public int All = -1;

            public int Counted;

            /// <summary>
            /// What this one trigger's signal read at the last sample.
            /// </summary>
            /// <remarks>
            /// Kept per trigger as well as gathered into one, because a screen listing triggers
            /// shows each of them saying what it is reading - and two triggers on one sequence
            /// would otherwise both light because one of them was true.
            /// </remarks>
            public bool Live;
        }

        readonly Dictionary<Trigger, Watch> _watching = new Dictionary<Trigger, Watch>();

        /// <summary>Forgets what a trigger read, for every trigger this sequence no longer has.</summary>
        void Forget(IReadOnlyList<Trigger> triggers)
        {
            _forgotten.Clear();

            foreach (Trigger held in _watching.Keys)
            {
                var kept = false;
                for (var i = 0; triggers != null && i < triggers.Count; i++)
                    kept = kept || (triggers[i] == held && triggers[i].Runs == _sequence.Number);

                if (!kept)
                    _forgotten.Add(held);
            }

            for (var i = 0; i < _forgotten.Count; i++)
                _watching.Remove(_forgotten[i]);
        }

        readonly List<Trigger> _forgotten = new List<Trigger>();

        /// <summary>Whether that trigger's own signal reads true at this instant.</summary>
        /// <remarks>
        /// Nothing where it has not been sampled yet, which is the honest answer: a trigger that
        /// has never been read has no reading.
        /// </remarks>
        public bool LiveNow(Trigger trigger)
        {
            Watch watch;
            return _watching.TryGetValue(trigger, out watch) && watch.Live;
        }

        Watch Watching(Trigger trigger)
        {
            Watch watch;
            if (_watching.TryGetValue(trigger, out watch))
                return watch;

            watch = new Watch();
            _watching.Add(trigger, watch);
            return watch;
        }

        bool SampleWhen(GridWorld world, WhenTrigger when, Watch watch)
        {
            var sources = Watched(when.Sources);
            var fired = false;
            var anySignalTrue = false;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var sourceId = sources[i];
                var entity = _bindings.Entity(sourceId);
                if (entity == null)
                {
                    anyMissing = true;
                    continue;
                }

                var definition = entity.Kind.FindEvent(when.EventId);
                if (definition == null)
                    continue;

                var now = Reads(world, entity, definition.PropertyId);
                if (now)
                    anySignalTrue = true;

                bool previous;
                if (watch.Signals.TryGetValue(sourceId, out previous))
                {
                    // No previous sample means the sequence has only just come into
                    // existence, and an edge needs two. Firing on the first tick
                    // would set every sequence off the moment it was created.
                    if (definition.Edge == Edge.Rising && !previous && now)
                        fired = true;
                    else if (definition.Edge == Edge.Falling && previous && !now)
                        fired = true;
                }

                watch.Signals[sourceId] = now;
            }

            TriggerSignalIsTrue = anySignalTrue;
            HasMissingSources = anyMissing;
            return fired;
        }

        /// <summary>
        /// Fires when the last of the sources reaches the state, and not again until
        /// one of them has left it.
        /// </summary>
        /// <remarks>
        /// Watching for several separate edges on the same tick would almost never
        /// fire, because the sources reach their state at different moments. What the
        /// sentence means is "the moment they are all there", so that is what is
        /// watched: one edge, on the whole condition.
        /// </remarks>
        bool SampleWhenAll(GridWorld world, WhenTrigger when, Watch watch)
        {
            var sources = Watched(when.Sources);
            var all = sources.Count > 0;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var entity = _bindings.Entity(sources[i]);
                if (entity == null)
                {
                    anyMissing = true;
                    all = false;
                    continue;
                }

                var definition = entity.Kind.FindEvent(when.EventId);
                if (definition == null)
                {
                    all = false;
                    continue;
                }

                var wanted = definition.Edge == Edge.Rising;
                if (Reads(world, entity, definition.PropertyId) != wanted)
                    all = false;
            }

            TriggerSignalIsTrue = all;
            HasMissingSources = anyMissing;

            // Unknown until the second sample, so a condition that is already true
            // when the sequence appears does not set it off.
            var now = all ? 1 : 0;
            var fired = watch.All == 0 && now == 1;
            watch.All = now;
            return fired;
        }

        bool SampleEvery(EveryTrigger every, Watch watch)
        {
            var interval = every.IntervalTicks < 1 ? 1 : every.IntervalTicks;
            watch.Counted++;
            if (watch.Counted < interval)
                return false;

            watch.Counted = 0;
            return true;
        }
    }
}
