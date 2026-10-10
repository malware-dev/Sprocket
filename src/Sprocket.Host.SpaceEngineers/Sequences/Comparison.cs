using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What an <see cref="If" /> tests: a member on some blocks compared with a value, on any of them, on all, or on their
    /// sum.
    /// </summary>
    /// <remarks>The member is one of the block's own values, or one of Sprocket's. It is read once, when the step is reached.</remarks>
    public sealed class Comparison
    {
        readonly List<string> _targets = new List<string>();
        readonly List<Literal> _arguments = new List<Literal>();

        /// <summary>The member's id.</summary>
        public string MemberId { get; set; } = "";

        /// <summary>The namespace the member is qualified by, or empty for a bare one.</summary>
        public string Provider { get; set; } = "";

        /// <summary>How the blocks are read: any of them, all, or their sum.</summary>
        public Quantifier Quantifier { get; set; }

        /// <summary>How the member is compared with <see cref="Value" />.</summary>
        public Relation Relation { get; set; }

        /// <summary>The names of the blocks it reads: a system's variables, or a system blueprint's slots.</summary>
        public IList<string> Targets => _targets;

        /// <summary>What the member is given, for one that takes arguments, in the order it declares its parameters.</summary>
        public IList<Literal> Arguments => _arguments;

        /// <summary>What the member is compared with.</summary>
        public Literal Value { get; set; }

        /// <summary>The value among <paramref name="actions" /> that the member names: Sprocket's own where it is qualified by <c>ext</c>, the block's own where it is bare; null where it names none.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            return EntityAction.Value(actions, Provider, MemberId);
        }
    }
}
