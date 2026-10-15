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
        readonly List<Literal> _arguments = new List<Literal>();
        string _provider;

        /// <summary>A step doing the action <paramref name="actionId" />, to no parts yet.</summary>
        public Act(string actionId)
        {
            ActionId = actionId;
        }

        /// <summary>The <see cref="World.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        /// <summary>Which of the three ways this reaches the block.</summary>
        /// <remarks>
        /// Part of what names the entry, alongside the id, and what the program writes it as: a bare
        /// head, <c>action</c> or <c>set</c>. Carried here rather than read off how many arguments the
        /// step holds, because arguments have no bearing on which it is.
        /// </remarks>
        public Nature Form { get; set; }

        /// <summary>What adds the member, where it is one a host adds rather than the game's own: <c>sprocket</c> for Sprocket's.</summary>
        /// <remarks>Kept as the program says it, so a member added by something this host does not have is never taken for one it does.</remarks>
        public string Provider
        {
            get { return _provider ?? (Form == Nature.Native ? BlockKinds.Own : ""); }
            set { _provider = value; }
        }

        /// <summary>Whether this writes one of the block's own values rather than performing an action.</summary>
        public bool Sets => Form == Nature.Value;

        /// <summary>The parts affected. All of them, together, in this step.</summary>
        public override IList<string> Targets => _targets;

        /// <summary>
        /// What the step gives its action, in the order the action declares it: an action's options, or the new value for a
        /// step that sets one.
        /// </summary>
        /// <remarks>
        /// Each carries the kind it was written as, which is the program's record of what somebody
        /// wrote. What the action takes is applied when the program is bound - see
        /// <see cref="World.EntityAction.Converts" />.
        /// <para>
        /// Empty for every action of Sprocket's own, and for the great majority of the game's.
        /// </para>
        /// </remarks>
        public IList<Literal> Arguments => _arguments;

        /// <summary>Whether the step has both parts to act on and an action to do to them.</summary>
        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);

        /// <summary>The entry among these that this step names, or null where there is none.</summary>
        /// <remarks>A value is found in its namespace, and only where a step can set it.</remarks>
        public EntityAction Names(IList<EntityAction> actions)
        {
            if (Form == Nature.Value)
            {
                var value = EntityAction.Value(actions, Provider, ActionId);
                return value != null && value.Sets ? value : null;
            }

            if (Form == Nature.Native && Provider != BlockKinds.Own)
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
