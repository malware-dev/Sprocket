using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// What an <see cref="If" /> tests: a member on some blocks compared with a value, on any of them, on all, or on their
    /// sum.
    /// </summary>
    /// <remarks>The member is read once, when the step is reached.</remarks>
    public sealed class Comparison : BranchTest
    {
        readonly List<string> _targets = new List<string>();
        readonly List<Literal> _arguments = new List<Literal>();

        /// <summary>The member this reads, as the program holds it: qualified by its namespace, or bare as it was written.</summary>
        public MemberKey Key { get; set; }

        /// <summary>The id of the member this reads, within its namespace.</summary>
        /// <remarks>Setting it keeps the namespace; the member is still named by <see cref="Key" />.</remarks>
        public string MemberId
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

        /// <summary>How the blocks are read: any of them, all, or their sum.</summary>
        public Quantifier Quantifier { get; set; }

        /// <summary>How the member is compared with <see cref="Value" />.</summary>
        public Relation Relation { get; set; }

        /// <summary>The names of the blocks it reads: a system's variables, or a system blueprint's slots.</summary>
        public override IList<string> Targets => _targets;

        /// <summary>What the member is given, for one that takes arguments, in the order it declares its parameters.</summary>
        public IList<Literal> Arguments => _arguments;

        /// <summary>What the member is compared with.</summary>
        public Literal Value { get; set; }

        /// <summary>A copy of this test, to change one thing of as an edit.</summary>
        public Comparison Copy()
        {
            var copy = new Comparison
            {
                Key = Key,
                Quantifier = Quantifier,
                Relation = Relation,
                Value = Value
            };

            copy._targets.AddRange(_targets);
            copy._arguments.AddRange(_arguments);
            return copy;
        }

        /// <summary>The value among <paramref name="actions" /> that the member names; null where it names none, or a bare name meets more than one.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            return EntityAction.Value(actions, Key);
        }

        /// <summary>Whether this test's bare name meets more than one value among <paramref name="actions" />, so it names none of them.</summary>
        public bool Ambiguous(IList<EntityAction> actions)
        {
            return EntityAction.Ambiguous(actions, Key, true);
        }
    }
}
