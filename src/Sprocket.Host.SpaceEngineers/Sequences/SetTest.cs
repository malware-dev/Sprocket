namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a <see cref="Branch" /> tests, changed; an otherwise given a test becomes an otherwise if.</summary>
    sealed class SetTest : SequenceEdit
    {
        readonly int _index;
        readonly Comparison _was;
        readonly Comparison _now;

        /// <summary>The test of the line at <paramref name="index" />, changed from <paramref name="was" /> to <paramref name="now" />.</summary>
        public SetTest(int index, Comparison was, Comparison now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        public override Change Change => Change.Step;

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);
        }

        void Put(Sequence sequence, Comparison test)
        {
            var branch = sequence.Steps[_index] as Branch;
            if (branch != null)
                branch.Test = test;
        }
    }
}
