using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What somebody settled on: the things to act on, and what should happen to them.
    /// </summary>
    /// <remarks>
    /// Handed back once, when the picking is done, and can still be refused then.
    /// </remarks>
    public sealed class PickAnswer
    {
        readonly List<long> _targets = new List<long>();
        readonly List<Literal> _arguments = new List<Literal>();

        /// <summary>An answer acting on <paramref name="targets" /> with <paramref name="chosen" />, read from them as <paramref name="quantifier" /> says, with no arguments.</summary>
        public PickAnswer(IList<long> targets, string chosen, Quantifier quantifier)
            : this(targets, chosen, quantifier, null)
        {
        }

        /// <summary>
        /// An answer acting on <paramref name="targets" /> with <paramref name="chosen" />, with the values <paramref name="arguments" />,
        /// reaching the blocks the way <paramref name="form" /> says, on the facet of each called <paramref name="facet" /> where
        /// what was chosen is a member of a type the host provides.
        /// </summary>
        public PickAnswer(IList<long> targets, string chosen, Quantifier quantifier, IList<Literal> arguments,
            ReachKind form = ReachKind.Native, string facet = "")
        {
            for (var i = 0; i < targets.Count; i++)
                _targets.Add(targets[i]);

            if (arguments != null)
            {
                for (var i = 0; i < arguments.Count; i++)
                    _arguments.Add(arguments[i]);
            }

            Chosen = chosen;
            Quantifier = quantifier;
            Form = form;
            Facet = facet ?? "";
        }

        /// <summary>The entities what was chosen acts on.</summary>
        public IList<long> Targets => _targets;

        /// <summary>
        /// What was given for the action, where it takes options or sets a value, and empty otherwise.
        /// </summary>
        public IList<Literal> Arguments => _arguments;

        /// <summary>The event or action that was chosen.</summary>
        public string Chosen { get; private set; }

        /// <summary>The name, shared by every block, of the facet a member of a type the host provides acts on; empty for any other.</summary>
        public string Facet { get; private set; }

        /// <summary>Which of the three ways what was chosen reaches the blocks.</summary>
        public ReachKind Form { get; private set; }

        /// <summary>How the blocks are read: any one of them, all of them together, or their sum.</summary>
        public Quantifier Quantifier { get; private set; }
    }
}
