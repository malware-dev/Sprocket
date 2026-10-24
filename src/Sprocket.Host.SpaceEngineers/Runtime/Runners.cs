using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>The runners a program has: one for each sequence on each installation of its logic system, and one for each trigger that acts itself.</summary>
    public sealed class Runners
    {
        readonly AutomationRunner _owner;
        readonly Automation _automation;
        readonly Dictionary<long, SequenceRunner> _runners = new Dictionary<long, SequenceRunner>();
        readonly Dictionary<long, SequenceRunner> _acting = new Dictionary<long, SequenceRunner>();
        readonly Dictionary<Trigger, Sequence> _alone = new Dictionary<Trigger, Sequence>();
        readonly HashSet<long> _keptActing = new HashSet<long>();
        readonly HashSet<Trigger> _keptTriggers = new HashSet<Trigger>();
        readonly List<Trigger> _goneTriggers = new List<Trigger>();
        readonly List<long> _stale = new List<long>();
        readonly List<long> _live = new List<long>();
        int _aloneIds;

        /// <summary>The runners of <paramref name="automation" />, each owned by <paramref name="owner" />.</summary>
        public Runners(AutomationRunner owner, Automation automation)
        {
            _owner = owner;
            _automation = automation;
        }

        /// <summary>The key a runner of sequence <paramref name="sequenceId" /> on installation <paramref name="instanceNumber" /> is kept under.</summary>
        public static long Key(int sequenceId, int instanceNumber)
        {
            return ((long)sequenceId << 20) | (uint)instanceNumber;
        }

        /// <summary>The runner kept under <paramref name="key" />, of a sequence or of a trigger's own act, or null.</summary>
        public SequenceRunner Get(long key)
        {
            SequenceRunner runner;
            if (_runners.TryGetValue(key, out runner))
                return runner;

            return _acting.TryGetValue(key, out runner) ? runner : null;
        }

        /// <summary>
        /// What runs the act a trigger does itself: a sequence with no number and that act for its one step, or null
        /// where the trigger runs or stops a sequence instead.
        /// </summary>
        /// <remarks>Kept per trigger for the life of the program.</remarks>
        public Sequence Alone(AutomationSystem system, Trigger trigger)
        {
            var act = trigger.Acts;
            if (act == null)
                return null;

            Sequence alone;
            if (!_alone.TryGetValue(trigger, out alone))
            {
                alone = new Sequence(--_aloneIds);
                alone.System = system;
                _alone.Add(trigger, alone);
            }

            if (alone.Steps.Count != 1 || alone.Steps[0] != act)
            {
                alone.Steps.Clear();
                alone.Steps.Add(act);
            }

            return alone;
        }

        /// <summary>
        /// Keeps the set of runners matching the sequences and the ones of each system.
        /// </summary>
        /// <remarks>Done on the tick after the program says it gained or lost something, and on no other.</remarks>
        public void Reconcile()
        {
            _live.Clear();

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
                        var key = Key(sequences[q].Id, instances[n].Number);
                        _live.Add(key);

                        if (_runners.ContainsKey(key))
                            continue;

                        var runner = new SequenceRunner(sequences[q], instances[n], _owner,
                            Remembered(system, instances[n].Number, sequences[q].Number));

                        // What a run step reaches: the same system, the same one of it, by number.
                        var owner = system;
                        var one = instances[n];
                        var caller = sequences[q].Number;
                        runner.Calls = (number, stops) =>
                        {
                            var called = owner.FindSequence(number);
                            if (called == null)
                                return;

                            if (stops)
                                _owner.Halt(called, one, StopCause.Sequence, caller);
                            else
                                _owner.Call(called, one);
                        };

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

            foreach (var staleKey in _stale)
            {
                Forget(_runners[staleKey]);
                _runners.Remove(staleKey);
            }
        }

        /// <summary>
        /// What the world remembers of sequence <paramref name="sequence" /> on installation <paramref name="instance" /> of
        /// <paramref name="system" />, kept from now on where it remembers nothing yet.
        /// </summary>
        /// <remarks>Asked as a runner is made, which is when the program gains something, never on a tick.</remarks>
        RunMemory Remembered(AutomationSystem system, int instance, int sequence)
        {
            var runs = _automation.Memory.Runs;
            foreach (var run in runs)
            {
                if (run.System == system.Identity && run.Instance == instance && run.Sequence == sequence)
                    return run;
            }

            var made = new RunMemory { System = system.Identity, Instance = instance, Sequence = sequence };
            runs.Add(made);
            return made;
        }

        /// <summary>Starts a match of the triggers, keeping none of their runners until each is kept again.</summary>
        public void Begin()
        {
            _keptActing.Clear();
            _keptTriggers.Clear();
        }

        /// <summary>Keeps what <paramref name="trigger" /> acts with, it being in the program still.</summary>
        public void KeepTrigger(Trigger trigger)
        {
            _keptTriggers.Add(trigger);
        }

        /// <summary>Keeps the runner of <paramref name="alone" />, the act of the trigger at <paramref name="place" /> of <paramref name="system" />, on <paramref name="instance" />, making it where there is none.</summary>
        public void Keep(AutomationSystem system, int place, Sequence alone, SystemInstance instance)
        {
            _keptActing.Add(Acting(system, place, alone, instance));
        }

        /// <summary>Retires the runners of the triggers not kept since <see cref="Begin" />, and forgets the acts of the triggers that are gone.</summary>
        public void Sweep()
        {
            _stale.Clear();
            foreach (var pair in _acting)
            {
                if (!_keptActing.Contains(pair.Key))
                    _stale.Add(pair.Key);
            }

            foreach (var staleKey in _stale)
            {
                Forget(_acting[staleKey]);
                _acting.Remove(staleKey);
            }

            // A trigger that has gone takes what ran its act with it; one put back by a step back is given another.
            _goneTriggers.Clear();
            foreach (var trigger in _alone.Keys)
            {
                if (!_keptTriggers.Contains(trigger))
                    _goneTriggers.Add(trigger);
            }

            foreach (var gone in _goneTriggers)
                _alone.Remove(gone);
        }

        /// <summary>Tells every runner a switch was flipped, so a run of what is switched off now stops; the sequences' first.</summary>
        public void Switched()
        {
            foreach (var runner in _runners.Values)
                runner.Switched();

            foreach (var runner in _acting.Values)
                runner.Switched();
        }

        /// <summary>Takes every runner off the rota and lets go of them all, so each is made again from the construct's memory as it stands.</summary>
        /// <remarks>For a copy whose memory was put back in place from the one that lent it; the next match of the program makes them.</remarks>
        public void Forget()
        {
            Retire();
            _runners.Clear();
            _acting.Clear();
            _alone.Clear();
            _keptActing.Clear();
            _keptTriggers.Clear();
            _goneTriggers.Clear();
            _stale.Clear();
            _live.Clear();
        }

        /// <summary>Takes every runner off the rota, the sequences' first.</summary>
        public void Retire()
        {
            foreach (var runner in _runners.Values)
                runner.Retire();

            foreach (var runner in _acting.Values)
                runner.Retire();
        }

        /// <summary>Gives every runner what <paramref name="bindings" /> says is gone, or may have been swapped, among the blocks it names, the sequences' first.</summary>
        public void Look(Bindings bindings)
        {
            foreach (var runner in _runners.Values)
                runner.Faults.Looked = SequenceSummary.Looked(bindings, runner.Sequence, runner.Instance);

            foreach (var runner in _acting.Values)
                runner.Faults.Looked = SequenceSummary.Looked(bindings, runner.Sequence, runner.Instance);
        }

        /// <summary>The key of the runner for the own act of trigger <paramref name="trigger" /> of <paramref name="system" /> on <paramref name="instance" />, made where there is none.</summary>
        /// <remarks>
        /// Remembered as a sequence numbered minus the trigger's place counted from one, which no sequence is; the place is
        /// written again on every check.
        /// </remarks>
        long Acting(AutomationSystem system, int trigger, Sequence alone, SystemInstance instance)
        {
            var key = Key(alone.Id, instance.Number);
            SequenceRunner runner;
            if (!_acting.TryGetValue(key, out runner))
            {
                runner = new SequenceRunner(alone, instance, _owner, Remembered(system, instance.Number, -(trigger + 1)));
                _acting.Add(key, runner);
            }

            runner.Memory.Sequence = -(trigger + 1);
            return key;
        }

        /// <summary>Retires a runner whose sequence or trigger is gone, and forgets what it remembered.</summary>
        void Forget(SequenceRunner runner)
        {
            runner.Retire();
            _automation.Memory.Runs.Remove(runner.Memory);
        }
    }
}
