using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>The runners a program has: one for each sequence on each installation of its logic system.</summary>
    public sealed class Runners
    {
        readonly AutomationRunner _owner;
        readonly Automation _automation;
        readonly Dictionary<long, SequenceRunner> _runners = new Dictionary<long, SequenceRunner>();
        readonly List<long> _stale = new List<long>();
        readonly List<long> _live = new List<long>();

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

        /// <summary>The runner kept under <paramref name="key" />, or null.</summary>
        public SequenceRunner Get(long key)
        {
            SequenceRunner runner;
            return _runners.TryGetValue(key, out runner) ? runner : null;
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

        /// <summary>Tells every runner a switch was flipped, so a run of what is switched off now stops.</summary>
        public void Switched()
        {
            foreach (var runner in _runners.Values)
                runner.Switched();
        }

        /// <summary>Takes every runner off the rota and lets go of them all, so each is made again from the construct's memory as it stands.</summary>
        /// <remarks>For a copy whose memory was put back in place from the one that lent it; the next match of the program makes them.</remarks>
        public void Forget()
        {
            Retire();
            _runners.Clear();
            _stale.Clear();
            _live.Clear();
        }

        /// <summary>Takes every runner off the rota.</summary>
        public void Retire()
        {
            foreach (var runner in _runners.Values)
                runner.Retire();
        }

        /// <summary>Gives every runner what <paramref name="bindings" /> says is gone, or may have been swapped, among the blocks it names.</summary>
        public void Look(Bindings bindings)
        {
            foreach (var runner in _runners.Values)
                runner.Faults.Looked = SequenceSummary.Looked(bindings, runner.Sequence, runner.Instance);
        }

        /// <summary>Retires a runner whose sequence is gone, and forgets what it remembered.</summary>
        void Forget(SequenceRunner runner)
        {
            runner.Retire();
            _automation.Memory.Runs.Remove(runner.Memory);
        }
    }
}
