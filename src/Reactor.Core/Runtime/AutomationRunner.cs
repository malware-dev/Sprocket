using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>
    /// Runs everything, one tick at a time.
    /// </summary>
    /// <remarks>
    /// There is no build, no run and no stop. The machine is always live, and edits
    /// take effect on the next tick - a sequence added while the ship is working
    /// simply begins working too. Removing the gap between writing something and
    /// seeing it happen is what turns "what will this do?" into something the user
    /// can answer by looking.
    /// </remarks>
    public sealed class AutomationRunner
    {
        readonly Dictionary<int, SequenceRunner> _runners = new Dictionary<int, SequenceRunner>();
        readonly List<int> _stale = new List<int>();
        readonly Automation _automation;
        readonly IWorld _world;

        public AutomationRunner(Automation automation, IWorld world)
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

        /// <summary>Advances everything to the given frame.</summary>
        public void Tick(int tick)
        {
            CurrentTick = tick;
            Reconcile();

            var sequences = _automation.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                SequenceRunner runner;
                if (_runners.TryGetValue(sequences[i].Id, out runner))
                    runner.Tick(_world, CurrentTick);
            }
        }

        /// <summary>Live state for a sequence, or null if it has never been ticked.</summary>
        public SequenceRunner Get(Sequence sequence)
        {
            SequenceRunner runner;
            return _runners.TryGetValue(sequence.Id, out runner) ? runner : null;
        }

        /// <summary>Starts a named sequence, as a trigger or another sequence would.</summary>
        public void Call(Sequence sequence)
        {
            var runner = Get(sequence);
            if (runner != null)
                runner.Call(_world, CurrentTick);
        }

        void Reconcile()
        {
            var sequences = _automation.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (!_runners.ContainsKey(sequences[i].Id))
                    _runners.Add(sequences[i].Id, new SequenceRunner(sequences[i]));
            }

            if (_runners.Count == sequences.Count)
                return;

            _stale.Clear();
            foreach (var pair in _runners)
            {
                if (_automation.FindSequence(pair.Key) == null)
                    _stale.Add(pair.Key);
            }

            for (var i = 0; i < _stale.Count; i++)
                _runners.Remove(_stale[i]);
        }
    }
}
