namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a <see cref="Branch" /> tests, changed; an otherwise given a test becomes an otherwise if.</summary>
    public sealed class SetTest : SequenceEdit
    {
        readonly int _index;
        readonly Comparison _was;
        readonly Comparison _now;
        readonly Swap _swap;

        /// <summary>The test of the line at <paramref name="index" />, changed from <paramref name="was" /> to <paramref name="now" />, making <paramref name="swap" /> where a slot keeps its place with another block.</summary>
        public SetTest(int index, Comparison was, Comparison now, Swap swap = null)
        {
            _swap = swap;
            _index = index;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.Step;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            if (_swap != null)
                _swap.Apply();

            Put(sequence, _now);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);

            if (_swap != null)
                _swap.Undo();
        }

        void Put(Sequence sequence, Comparison test)
        {
            var branch = sequence.Steps[_index] as Branch;
            if (branch != null)
                branch.Test = test;
        }
    }
}
