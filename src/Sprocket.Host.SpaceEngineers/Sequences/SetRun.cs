namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Which sequence a step runs or stops, changed.</summary>
    public sealed class SetRun : SequenceEdit
    {
        readonly int _index;
        readonly int _was;
        readonly int _now;

        /// <summary>Changes which sequence the step at <paramref name="index" /> runs or stops, from <paramref name="was" /> to <paramref name="now" />.</summary>
        public SetRun(int index, int was, int now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.Step;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

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

        void Put(Sequence sequence, int number)
        {
            var run = sequence.Steps[_index] as Run;
            if (run != null)
                run.Number = number;
        }
    }
}
