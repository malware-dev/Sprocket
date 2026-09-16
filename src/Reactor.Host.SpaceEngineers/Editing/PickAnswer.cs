using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// What somebody settled on: the things to act on, and what should happen to them.
    /// </summary>
    /// <remarks>
    /// Handed back once, when they say they are done, rather than written into the sequence as the
    /// picking goes along. There is therefore a moment where an answer exists and can be refused.
    /// </remarks>
    sealed class PickAnswer
    {
        readonly List<long> _targets = new List<long>();

        public PickAnswer(IList<long> targets, string chosen, bool all)
        {
            for (var i = 0; i < targets.Count; i++)
                _targets.Add(targets[i]);

            Chosen = chosen;
            All = all;
        }

        public IList<long> Targets => _targets;

        /// <summary>The event or action that was chosen.</summary>
        public string Chosen { get; private set; }

        /// <summary>Whether it takes all of them together, rather than any one.</summary>
        public bool All { get; private set; }
    }
}
