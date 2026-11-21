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

        /// <summary>A step doing the action <paramref name="actionId" />, written bare, to no parts yet.</summary>
        public Act(string actionId)
        {
            Key = MemberKey.Bare(actionId);
        }

        /// <summary>A step doing the member <paramref name="key" />, to no parts yet.</summary>
        public Act(MemberKey key)
        {
            Key = key;
        }

        /// <summary>The member this does, as the program holds it: qualified by its namespace, or bare as it was written.</summary>
        public MemberKey Key { get; set; }

        /// <summary>The id of the member this does, within its namespace.</summary>
        /// <remarks>Setting it keeps the namespace; the member is still named by <see cref="Key" />.</remarks>
        public string ActionId
        {
            get { return Key.Id; }
            set { Key = Key.WithId(value); }
        }

        /// <summary>The namespace the member is written qualified by, or empty where it is written bare.</summary>
        /// <remarks>Setting it keeps the id; the member is still named by <see cref="Key" />.</remarks>
        public string Provider
        {
            get { return Key.Namespace; }
            set { Key = new MemberKey(value, Key.Id); }
        }

        /// <summary>Which of the three ways this reaches the block, as it is written: a value with <c>set</c>, and otherwise an action.</summary>
        /// <remarks>Of an action, the form its bound member has says whether it is the game's or a host's.</remarks>
        public ReachKind Form { get; set; }

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
        public bool HasAction => Targets.Count > 0 && !Key.IsEmpty;

        /// <summary>The entry among these that this step names, or null where there is none, or a bare name meets more than one.</summary>
        /// <remarks>A value is returned only where a step can set it.</remarks>
        public EntityAction Names(IList<EntityAction> actions)
        {
            var named = EntityAction.Find(actions, Key, Sets);
            return named != null && (!Sets || named.Sets) ? named : null;
        }

        /// <summary>Whether this step's bare name meets more than one entry among these, so it names none of them.</summary>
        public bool Ambiguous(IList<EntityAction> actions)
        {
            return EntityAction.Ambiguous(actions, Key, Sets);
        }
    }
}
