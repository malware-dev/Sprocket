
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    public sealed class Rename : SequenceEdit
    {
        readonly string _was;
        readonly string _now;

        public Rename(string was, string now)
        {
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.Rename;

        /// <inheritdoc />
        public override bool ChangesShape => false;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            sequence.Name = _now;
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            sequence.Name = _was;
        }
    }
}
