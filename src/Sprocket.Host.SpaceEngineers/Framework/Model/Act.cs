using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A step that does something to some of the system's parts.
    /// </summary>
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

        /// <summary>The <see cref="Game.Members.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        /// <summary>Which of the three ways this reaches the block.</summary>
        public ReachKind Form { get; set; }

        /// <summary>What adds the member, where it is one a host adds rather than the game's own: <c>sprocket</c> for Sprocket's.</summary>
        public string Provider
        {
            get { return _provider ?? (Form == ReachKind.Native ? BlockKinds.Own : ""); }
            set { _provider = value; }
        }

        /// <summary>Whether this writes one of the block's own values rather than performing an action.</summary>
        public bool Sets => Form == ReachKind.Value;

        /// <summary>The parts the step acts on, all together.</summary>
        public override IList<string> Targets => _targets;

        /// <summary>
        /// What the step gives its action, in the order the action declares it: an action's options, or the new value for a
        /// step that sets one.
        /// </summary>
        public override IList<Literal> Arguments => _arguments;

        /// <summary>Whether the step has both parts to act on and an action to do to them.</summary>
        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);

        /// <summary>The entry among these that this step names, or null where there is none.</summary>
        /// <remarks>A value is returned only where a step can set it.</remarks>
        public EntityAction Names(IList<EntityAction> actions)
        {
            if (Form == ReachKind.Value)
            {
                var value = EntityAction.Value(actions, Provider, ActionId);
                return value != null && value.Sets ? value : null;
            }

            if (Form == ReachKind.Native && Provider != BlockKinds.Own)
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
