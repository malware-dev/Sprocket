
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    sealed class SetWait : SequenceEdit
    {
        readonly int _index;
        readonly int _was;
        readonly int _now;

        public SetWait(int index, int was, int now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.Wait;

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

        void Put(Sequence sequence, int ticks)
        {
            var wait = sequence.Steps[_index] as Wait;
            if (wait != null)
                wait.Ticks = ticks;
        }
    }
}
