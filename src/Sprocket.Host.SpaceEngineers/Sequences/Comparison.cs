using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What an <see cref="If" /> tests: whether a member on some blocks equals a value, on any of them or on all.
    /// </summary>
    /// <remarks>
    /// The member is one <c>set</c> writes, or a moment, whose value is a flag: on where the block is in that state. It is
    /// read once, when the step is reached.
    /// </remarks>
    public sealed class Comparison
    {
        readonly List<string> _targets = new List<string>();

        /// <summary>The member's id: a value's, or a moment's.</summary>
        public string MemberId { get; set; } = "";

        /// <summary>The namespace the member is qualified by, or empty for a bare one.</summary>
        public string Provider { get; set; } = "";

        /// <summary>Whether every block's member must equal the value, rather than one block's.</summary>
        public bool RequiresAll { get; set; }

        /// <summary>The names of the blocks it reads: a system's variables, or a system blueprint's slots.</summary>
        public IList<string> Targets => _targets;

        /// <summary>What the member is compared with.</summary>
        public Literal Value { get; set; }

        /// <summary>Whether the member is a moment, qualified by <c>ext</c>, rather than a value.</summary>
        public bool IsMoment => Provider == BlockKinds.Own;

        /// <summary>The value among <paramref name="actions" /> that the member names, or null where it names none or is a moment.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            if (IsMoment)
                return null;

            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == MemberId && actions[i].Form == Form.Value)
                    return actions[i];
            }

            return null;
        }

        /// <summary>The moment as a block of <paramref name="kind" /> has it, or null where it has none that is a state or the member is a value.</summary>
        public EntityEvent Moment(EntityKind kind)
        {
            var moment = IsMoment ? kind.FindEvent(MemberId) : null;
            return moment != null && moment.IsState ? moment : null;
        }
    }
}
