using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.Core.World;

namespace Reactor.Host.SpaceEngineers.Core.Runtime
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

        List<string> ActedOn(IList<string> roles)
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

        List<string> Watched(IList<string> roles)
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

        public Sequence Sequence => _sequence;

        public bool IsRunning { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep => _stepIndex;

        /// <summary>True while the sequence is held at an await or a wait.</summary>
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

        public void Tick(IWorld world, Bindings bindings, int tick)
        {
            _bindings = bindings;

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
                Advance(world);
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
        public void Call(IWorld world, Bindings bindings, int tick)
        {
            _bindings = bindings;

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
        public bool Settled(IWorld world, Act step)
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
        void Advance(IWorld world)
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
                    if (!Finished(world))
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

        /// <summary>Whether everything started since the last await has got where it was going.</summary>
        bool Finished(IWorld world)
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

        void Execute(IWorld world, Act step)
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
        bool SampleTrigger(IWorld world)
        {
            var anySignal = false;
            var anyMissing = false;
            var fired = false;

            var system = _sequence.System;
            var triggers = system == null ? null : system.Triggers;
            for (var i = 0; triggers != null && i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger.Runs != _sequence.Number)
                    continue;

                if (Sampled(world, trigger, Watching(trigger)))
                    fired = true;

                anySignal = anySignal || TriggerSignalIsTrue;
                anyMissing = anyMissing || HasMissingSources;
            }

            TriggerSignalIsTrue = anySignal;
            HasMissingSources = anyMissing;
            return fired;
        }

        bool Sampled(IWorld world, Trigger trigger, Watch watch)
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
        }

        readonly Dictionary<Trigger, Watch> _watching = new Dictionary<Trigger, Watch>();

        Watch Watching(Trigger trigger)
        {
            Watch watch;
            if (_watching.TryGetValue(trigger, out watch))
                return watch;

            watch = new Watch();
            _watching.Add(trigger, watch);
            return watch;
        }

        bool SampleWhen(IWorld world, WhenTrigger when, Watch watch)
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
                if (definition == null || definition.Answered)
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
        bool SampleWhenAll(IWorld world, WhenTrigger when, Watch watch)
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

                // An answered one has nothing to read, so "all of them" cannot include it: what
                // would make it true arrives by being invoked, not by being watched.
                if (definition == null || definition.Answered)
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
