
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>How long a wait step waits.</summary>
    public sealed class SetWait : SequenceEdit
    {
        readonly int _index;
        readonly Duration _was;
        readonly Duration _now;

        /// <summary>The wait at <paramref name="index" /> made <paramref name="now" /> long from <paramref name="was" />.</summary>
        public SetWait(int index, Duration was, Duration now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Wait;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
            name.Was = _was;
            name.Now = _now;
        }

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

        void Put(Sequence sequence, Duration length)
        {
            var wait = sequence.Steps[_index] as Wait;
            if (wait != null)
                wait.Length = length;
        }
    }
}
