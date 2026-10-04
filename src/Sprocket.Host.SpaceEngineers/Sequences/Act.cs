using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that does something to some of the system's parts.
    /// </summary>
    /// <remarks>
    /// It starts the action and the sequence carries straight on. Holding until the doors have
    /// actually shut is <see cref="Await"/>, which is a line of its own - so acting on several
    /// things at once needs nothing said, and waiting is what has to be asked for.
    /// </remarks>
    public sealed class Act : Step
    {
        readonly List<string> _targets = new List<string>();

        public Act(string actionId)
        {
            ActionId = actionId;
        }

        /// <summary>The <see cref="World.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        /// <summary>Which of the three ways this reaches the block.</summary>
        /// <remarks>
        /// Part of what names the entry, alongside the id, and what the program writes it as: a bare
        /// head, <c>action</c> or <c>set</c>. Carried here rather than read off how many values the
        /// step holds, because values have no bearing on which it is.
        /// </remarks>
        public Form Form { get; set; }

        /// <summary>What adds the member, where it is one a host adds rather than the game's own: <c>sprocket</c> for Sprocket's.</summary>
        /// <remarks>Kept as the program says it, so a member added by something this host does not have is never taken for one it does.</remarks>
        public string Provider { get; set; } = BlockKinds.Own;

        /// <summary>Whether this writes one of the block's own values rather than performing an action.</summary>
        public bool Sets => Form == Form.Value;

        /// <summary>The parts affected. All of them, together, in this step.</summary>
        public override IList<string> Targets => _targets;

        /// <summary>
        /// The values the action has to be given, in the order it declares them.
        /// </summary>
        /// <remarks>
        /// Each carries the kind it was written as, which is the program's record of what somebody
        /// wrote. What the action takes is applied when the program is bound - see
        /// <see cref="World.EntityAction.Converts" />.
        /// <para>
        /// Empty for every action of Sprocket's own, and for the great majority of the game's.
        /// </para>
        /// </remarks>
        public IList<Literal> Values => _values;

        readonly List<Literal> _values = new List<Literal>();

        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);

        /// <summary>The entry among these that this step names, or null where there is none.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            if (Form == Form.Native && Provider != BlockKinds.Own)
                return null;

            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == ActionId && actions[i].Form == Form)
                    return actions[i];
            }

            return null;
        }
    }
}
