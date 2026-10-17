
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Gives a sequence another name.</summary>
    public sealed class RenameSequence : SequenceEdit
    {
        readonly string _was;
        readonly string _now;

        /// <summary>Renames a sequence called <paramref name="was" /> to <paramref name="now" />.</summary>
        public RenameSequence(string was, string now)
        {
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Rename;

        /// <inheritdoc />
        public override bool ChangesShape => false;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <summary>A sequence is renamed whether or not its logic system runs, so taking the name back waits for nothing either.</summary>
        public override bool Held(Sequence sequence, bool back)
        {
            return false;
        }

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
