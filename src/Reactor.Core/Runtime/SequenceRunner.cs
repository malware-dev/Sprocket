using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>Why an action did not happen.</summary>
    /// <remarks>
    /// Kept structurally rather than as a sentence, so the editor can word it. This
    /// is the raw material for "why didn't my door open?" - the one question a
    /// non-programmer actually asks, and the one almost no environment answers.
    /// </remarks>
    /// <summary>A step that was passed over because its "only if" was not true.</summary>
    /// <remarks>
    /// Recorded structurally rather than as a sentence, so the editor can word it.
    /// This is the raw material for "why didn't my door open?" - the one question a
    /// non-programmer actually asks, and the one almost no environment answers.
    /// </remarks>
    public sealed class SkippedStep
    {
        public SkippedStep(int stepIndex, int beat)
        {
            StepIndex = stepIndex;
            Beat = beat;
        }

        public int StepIndex { get; private set; }

        public int Beat { get; private set; }
    }

    /// <summary>
    /// Runs one sequence, and carries everything the editor needs to show what it is
    /// doing right now.
    /// </summary>
    public sealed class SequenceRunner
    {
        readonly Dictionary<long, bool> _lastSignal = new Dictionary<long, bool>();
        readonly Sequence _sequence;

        int _stepIndex = -1;
        int _gateBeats;
        int _everyCounter;
        bool _awaitingCompletion;

        public SequenceRunner(Sequence sequence)
        {
            _sequence = sequence;
            LastFiredBeat = -1;
            LastSkippedBeat = -1;
        }

        public Sequence Sequence { get { return _sequence; } }

        public bool IsRunning { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep { get { return _stepIndex; } }

        /// <summary>True while the current step's gate has not opened.</summary>
        public bool IsWaiting { get; private set; }

        public int LastFiredBeat { get; private set; }

        public int FiredCount { get; private set; }

        /// <summary>
        /// How often the trigger fired while the sequence was already running.
        /// </summary>
        /// <remarks>
        /// Re-triggering skips rather than queueing, restarting, or running a second
        /// copy. Skipping is the only option that cannot silently accumulate work,
        /// and it matches how people think about a machine that is busy. With real
        /// gates the wait is legible - "waiting for the inner door to shut" - so busy
        /// explains itself instead of being a mystery.
        /// </remarks>
        public int SkippedCount { get; private set; }

        public int LastSkippedBeat { get; private set; }

        /// <summary>Whether the watched signal is true right now.</summary>
        public bool TriggerSignalIsTrue { get; private set; }

        /// <summary>One or more listened-to devices are no longer on the construct.</summary>
        public bool HasMissingSources { get; private set; }

        /// <summary>The most recent step passed over by its guard, or null.</summary>
        public SkippedStep LastSkippedStep { get; private set; }

        public void Tick(IWorld world, int beat)
        {
            // Sample every beat, even while running or switched off. Edge detection
            // is stateful, so a skipped sample makes the next one wrong.
            var triggered = SampleTrigger(world);

            if (!_sequence.Enabled)
            {
                Stop();
                return;
            }

            if (IsRunning)
            {
                if (triggered)
                {
                    SkippedCount++;
                    LastSkippedBeat = beat;
                }
                Advance(world, beat);
                return;
            }

            if (triggered)
                Start(world, beat);
        }

        /// <summary>Starts the sequence by name, from another sequence or a trigger.</summary>
        public void Call(IWorld world, int beat)
        {
            if (!_sequence.Enabled)
                return;

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedBeat = beat;
                return;
            }

            Start(world, beat);
        }

        public void Stop()
        {
            IsRunning = false;
            IsWaiting = false;
            _stepIndex = -1;
            _gateBeats = 0;
            _awaitingCompletion = false;
        }

        /// <summary>
        /// Whether every device this step acted on has reached the state the action
        /// was reaching for.
        /// </summary>
        /// <remarks>
        /// An action that declares no destination - switching a light on - is done
        /// the instant it is invoked, so instant things stay instant and only things
        /// that genuinely take time are waited on.
        ///
        /// A device that never gets there holds the sequence indefinitely. That is
        /// the right failure: carrying on regardless is how a jammed door vents a
        /// ship. It does mean being visibly stuck has to be impossible to miss, which
        /// is why the step being waited on is reported rather than inferred.
        /// </remarks>
        public bool Settled(IWorld world, Step step)
        {
            if (!step.HasAction)
                return true;

            for (var i = 0; i < step.Targets.Count; i++)
            {
                var device = world.FindDevice(step.Targets[i]);
                if (device == null)
                    continue;

                var action = device.Kind.FindAction(step.ActionId);
                if (action == null || string.IsNullOrEmpty(action.LeadsToStateId))
                    continue;

                var state = device.Kind.FindState(action.LeadsToStateId);
                if (state == null)
                    continue;

                if (device.GetSignal(state.SignalId) != state.WhenSignal)
                    return false;
            }

            return true;
        }

        void Start(IWorld world, int beat)
        {
            LastFiredBeat = beat;
            FiredCount++;
            IsRunning = true;
            _stepIndex = 0;
            EnterStep();
            Advance(world, beat);
        }

        void EnterStep()
        {
            _gateBeats = 0;
            if (_stepIndex < 0 || _stepIndex >= _sequence.Steps.Count)
                return;

            var after = _sequence.Steps[_stepIndex].Gate as AfterGate;
            if (after != null)
                _gateBeats = after.Beats;
        }

        void Advance(IWorld world, int beat)
        {
            // Ungated steps all run in the same beat, so "close the door" followed by
            // "once it is shut, pump the air out" behaves the way it reads.
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
                if (guard != null && !Conditions.IsTrue(world, guard.Condition))
                {
                    // Passed over rather than waited on, and recorded so the user can
                    // be told which situation was not true.
                    LastSkippedStep = new SkippedStep(_stepIndex, beat);
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
                if (_gateBeats <= 0)
                    return true;
                _gateBeats--;
                return _gateBeats <= 0;
            }

            var once = step.Gate as OnceGate;
            if (once != null)
                return Conditions.IsTrue(world, once.Condition);

            return true;
        }

        void Execute(IWorld world, Step step)
        {
            if (!step.HasAction)
                return;

            for (var i = 0; i < step.Targets.Count; i++)
            {
                var device = world.FindDevice(step.Targets[i]);
                if (device == null)
                    continue;

                if (device.Kind.FindAction(step.ActionId) == null)
                    continue;

                world.Invoke(device, step.ActionId);
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
            var fired = false;
            var anySignalTrue = false;
            var anyMissing = false;

            for (var i = 0; i < when.Sources.Count; i++)
            {
                var sourceId = when.Sources[i];
                var device = world.FindDevice(sourceId);
                if (device == null)
                {
                    anyMissing = true;
                    continue;
                }

                var definition = device.Kind.FindEvent(when.EventId);
                if (definition == null)
                    continue;

                var now = device.GetSignal(definition.SignalId);
                if (now)
                    anySignalTrue = true;

                bool previous;
                if (_lastSignal.TryGetValue(sourceId, out previous))
                {
                    // No previous sample means the sequence has only just come into
                    // existence, and an edge needs two. Firing on the first beat
                    // would set every sequence off the moment it was created.
                    if (definition.Edge == SignalEdge.Rising && !previous && now)
                        fired = true;
                    else if (definition.Edge == SignalEdge.Falling && previous && !now)
                        fired = true;
                }

                _lastSignal[sourceId] = now;
            }

            TriggerSignalIsTrue = anySignalTrue;
            HasMissingSources = anyMissing;
            return fired;
        }

        bool SampleEvery(EveryTrigger every)
        {
            var interval = every.IntervalBeats < 1 ? 1 : every.IntervalBeats;
            _everyCounter++;
            if (_everyCounter < interval)
                return false;

            _everyCounter = 0;
            return true;
        }
    }
}
