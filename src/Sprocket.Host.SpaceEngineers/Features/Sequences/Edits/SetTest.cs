using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>What a <see cref="Branch" /> tests, changed; an otherwise given a test becomes an otherwise if.</summary>
    public sealed class SetTest : SequenceEdit
    {
        readonly int _index;
        readonly BranchTest _was;
        readonly BranchTest _now;

        /// <summary>The test of the line at <paramref name="index" />, changed from <paramref name="was" /> to <paramref name="now" />.</summary>
        public SetTest(int index, BranchTest was, BranchTest now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Step;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
        }

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);
        }

        void Put(Sequence sequence, BranchTest test)
        {
            var branch = sequence.Steps[_index] as Branch;
            if (branch != null)
                branch.Test = test;
        }
    }
}
