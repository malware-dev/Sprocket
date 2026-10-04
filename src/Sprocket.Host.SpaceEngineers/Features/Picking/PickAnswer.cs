using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What somebody settled on: the things to act on, and what should happen to them.
    /// </summary>
    /// <remarks>
    /// Handed back once, when they say they are done, rather than written into the sequence as the
    /// picking goes along. There is therefore a moment where an answer exists and can be refused.
    /// </remarks>
    public sealed class PickAnswer
    {
        readonly List<long> _targets = new List<long>();
        readonly List<Literal> _values = new List<Literal>();

        public PickAnswer(IList<long> targets, string chosen, bool all)
            : this(targets, chosen, all, null)
        {
        }

        public PickAnswer(IList<long> targets, string chosen, bool all, IList<Literal> values,
            Form form = Form.Native)
        {
            for (var i = 0; i < targets.Count; i++)
                _targets.Add(targets[i]);

            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                    _values.Add(values[i]);
            }

            Chosen = chosen;
            All = all;
            Form = form;
        }

        public IList<long> Targets => _targets;

        /// <summary>
        /// What was typed for the action, where it takes values, and empty otherwise.
        /// </summary>
        /// <remarks>
        /// Asked for on the button that finishes the picking, because an action that needs values is
        /// not finished without them - so they arrive with the rest of the answer rather than
        /// afterwards.
        /// </remarks>
        public IList<Literal> Values => _values;

        /// <summary>The event or action that was chosen.</summary>
        public string Chosen { get; private set; }

        /// <summary>Which of the three ways what was chosen reaches the blocks.</summary>
        public Form Form { get; private set; }

        /// <summary>Whether it takes all of them together, rather than any one.</summary>
        public bool All { get; private set; }
    }
}
