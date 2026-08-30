using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>
    /// Runs one sequence, and carries everything the editor needs to show what it is
    /// doing right now.
    /// </summary>
    public sealed class SequenceRunner
    {
        readonly Dictionary<long, bool> _lastSignal = new Dictionary<long, bool>();
        readonly Sequence _sequence;

        int _stepIndex = -1;
        int _gateTicks;
        int _everyCounter;
        int _allState = -1;
        bool _awaitingCompletion;

        public SequenceRunner(Sequence sequence) : this(sequence, null)
        {
        }

        /// <summary>
        /// One sequence, running for one of a system.
        /// </summary>
        /// <remarks>
        /// The same sequence runs once for each of a system on the ship, each resolving the
        /// parts the author built with into its own doors. Null means the parts are the
        /// blocks themselves, which is what the first one of a system does and what every
        /// system built before roles existed keeps doing.
        /// </remarks>
        public SequenceRunner(Sequence sequence, SystemInstance instance)
        {
            _sequence = sequence;
            _instance = instance;
            LastFiredTick = -1;
            LastSkippedTick = -1;
        }

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
        bool CanRun
        {
            get { return _sequence.CanRun && (_instance == null || _instance.Enabled); }
        }

        /// <summary>
        /// Scratch for turning the system's parts into this one's blocks.
        /// </summary>
        /// <remarks>
        /// Two, because the trigger is sampled on the same tick a step is acting and one
        /// buffer would have them overwrite each other. Reused rather than allocated, because
        /// this happens for every running sequence on every tick and core sits inside the
        /// game's update loop.
        /// </remarks>
        readonly List<long> _actedOn = new List<long>();
        readonly List<long> _watched = new List<long>();

        List<long> ActedOn(IList<long> roles)
        {
            _actedOn.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < roles.Count; i++)
                    _actedOn.Add(roles[i]);
            }
            else
            {
                _instance.Resolve(roles, _actedOn);
            }
            return _actedOn;
        }

        /// <summary>
        /// A third buffer: a gate is asked while a step is already being acted on.
        /// </summary>
        readonly List<long> _guarding = new List<long>();

        List<long> Guarding(Condition condition)
        {
            if (condition == null)
                return null;

            _guarding.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < condition.Entities.Count; i++)
                    _guarding.Add(condition.Entities[i]);
            }
            else
            {
                _instance.Resolve(condition.Entities, _guarding);
            }
            return _guarding;
        }

        List<long> Watched(IList<long> roles)
        {
            _watched.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < roles.Count; i++)
                    _watched.Add(roles[i]);
            }
            else
            {
                _instance.Resolve(roles, _watched);
            }
            return _watched;
        }

        public Sequence Sequence { get { return _sequence; } }

        public bool IsRunning { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep { get { return _stepIndex; } }

        /// <summary>True while the current step's gate has not opened.</summary>
        public bool IsWaiting { get; private set; }

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

        /// <summary>Whether the watched signal is true right now.</summary>
        public bool TriggerSignalIsTrue { get; private set; }

        /// <summary>One or more listened-to entities are no longer on the construct.</summary>
        public bool HasMissingSources { get; private set; }

        /// <summary>The most recent step passed over by its guard, or null.</summary>
        public SkippedStep LastSkippedStep { get; private set; }

        public void Tick(IWorld world, int tick)
        {
            // Sample every tick, even while running or switched off. Edge detection
            // is stateful, so a skipped sample makes the next one wrong.
            var triggered = SampleTrigger(world);

            if (!CanRun)
            {
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
                Advance(world, tick);
                return;
            }

            if (triggered)
                Start(world, tick);
        }

        /// <summary>Starts the sequence by name, from another sequence or a trigger.</summary>
        /// <remarks>
        /// The switch holds here. A parked sequence is parked for everything, including being
        /// called by another one.
        /// </remarks>
        public void Call(IWorld world, int tick)
        {
            if (!CanRun)
                return;

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                return;
            }

            Start(world, tick);
        }

        public void Stop()
        {
            IsRunning = false;
            IsWaiting = false;
            _stepIndex = -1;
            _gateTicks = 0;
            _awaitingCompletion = false;
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
        public bool Settled(IWorld world, Step step)
        {
            if (!step.HasAction)
                return true;

            var settling = ActedOn(step.Targets);
            for (var i = 0; i < settling.Count; i++)
            {
                var entity = world.FindEntity(settling[i]);
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
        static bool Reads(IWorld world, IEntity entity, string propertyId)
        {
            var property = entity.Kind.FindProperty(propertyId);
            var kind = property == null ? ValueKind.Flag : property.Kind;

            return world.Values.Flag(kind, entity.GetValue(propertyId));
        }

        void Start(IWorld world, int tick)
        {
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;
            _stepIndex = 0;
            EnterStep();
            Advance(world, tick);
        }

        void EnterStep()
        {
            _gateTicks = 0;
            if (_stepIndex < 0 || _stepIndex >= _sequence.Steps.Count)
                return;

            var after = _sequence.Steps[_stepIndex].Gate as AfterGate;
            if (after != null)
                _gateTicks = after.Ticks;
        }

        void Advance(IWorld world, int tick)
        {
            // Ungated steps all run in the same tick, so "close the door" followed by
            // "once it is shut, depressurize" behaves the way it reads.
            while (_stepIndex >= 0 && _stepIndex < _sequence.Steps.Count)
            {
                var step = _sequence.Steps[_stepIndex];

                // Holding here until the action has actually taken effect is what
                // makes a numbered list mean what it looks like it means.
                if (_awaitingCompletion)
                {
                    if (!Settled(world, step))
                    {
                        IsWaiting = true;
                        return;
                    }

                    _awaitingCompletion = false;
                    IsWaiting = false;
                    _stepIndex++;
                    EnterStep();
                    continue;
                }

                var guard = step.Gate as OnlyIfGate;
                if (guard != null && !Conditions.IsTrue(world, guard.Condition, Guarding(guard.Condition)))
                {
                    // Passed over rather than waited on, and recorded so the user can
                    // be told which situation was not true.
                    LastSkippedStep = new SkippedStep(_stepIndex, tick);
                    IsWaiting = false;
                    _stepIndex++;
                    EnterStep();
                    continue;
                }

                if (!GateOpen(world, step))
                {
                    IsWaiting = true;
                    return;
                }

                IsWaiting = false;
                Execute(world, step);

                if (step.WaitsForCompletion && !Settled(world, step))
                {
                    _awaitingCompletion = true;
                    IsWaiting = true;
                    return;
                }

                _stepIndex++;
                EnterStep();
            }

            Stop();
        }

        bool GateOpen(IWorld world, Step step)
        {
            if (step.Gate == null)
                return true;

            var after = step.Gate as AfterGate;
            if (after != null)
            {
                if (_gateTicks <= 0)
                    return true;
                _gateTicks--;
                return _gateTicks <= 0;
            }

            var once = step.Gate as OnceGate;
            if (once != null)
                return Conditions.IsTrue(world, once.Condition, Guarding(once.Condition));

            return true;
        }

        void Execute(IWorld world, Step step)
        {
            if (!step.HasAction)
                return;

            var acting = ActedOn(step.Targets);
            for (var i = 0; i < acting.Count; i++)
            {
                var entity = world.FindEntity(acting[i]);
                if (entity == null)
                    continue;

                if (entity.Kind.FindAction(step.ActionId) == null)
                    continue;

                world.Invoke(entity, step.ActionId);
            }
        }

        bool SampleTrigger(IWorld world)
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when != null)
                return SampleWhen(world, when);

            var every = _sequence.Trigger as EveryTrigger;
            if (every != null)
                return SampleEvery(every);

            return false;
        }

        bool SampleWhen(IWorld world, WhenTrigger when)
        {
            if (when.RequiresAll)
                return SampleWhenAll(world, when);

            var sources = Watched(when.Sources);
            var fired = false;
            var anySignalTrue = false;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var sourceId = sources[i];
                var entity = world.FindEntity(sourceId);
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
                if (_lastSignal.TryGetValue(sourceId, out previous))
                {
                    // No previous sample means the sequence has only just come into
                    // existence, and an edge needs two. Firing on the first tick
                    // would set every sequence off the moment it was created.
                    if (definition.Edge == Edge.Rising && !previous && now)
                        fired = true;
                    else if (definition.Edge == Edge.Falling && previous && !now)
                        fired = true;
                }

                _lastSignal[sourceId] = now;
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
        bool SampleWhenAll(IWorld world, WhenTrigger when)
        {
            var sources = Watched(when.Sources);
            var all = sources.Count > 0;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var entity = world.FindEntity(sources[i]);
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
            var fired = _allState == 0 && now == 1;
            _allState = now;
            return fired;
        }

        bool SampleEvery(EveryTrigger every)
        {
            var interval = every.IntervalTicks < 1 ? 1 : every.IntervalTicks;
            _everyCounter++;
            if (_everyCounter < interval)
                return false;

            _everyCounter = 0;
            return true;
        }
    }
}
