namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What an abort step says, and the level it shows at.</summary>
    public sealed class SetAbort : SequenceEdit
    {
        readonly int _index;
        readonly string _wasMessage;
        readonly AbortLevel _wasLevel;
        readonly string _nowMessage;
        readonly AbortLevel _nowLevel;

        /// <summary>The abort <paramref name="step" />, at <paramref name="index" />, made to say <paramref name="message" /> at <paramref name="level" />.</summary>
        public SetAbort(int index, Abort step, string message, AbortLevel level)
        {
            _index = index;
            _wasMessage = step.Message;
            _wasLevel = step.Level;
            _nowMessage = message ?? "";
            _nowLevel = level;
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
        public override bool Changes => _wasMessage != _nowMessage || _wasLevel != _nowLevel;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowMessage, _nowLevel);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasMessage, _wasLevel);
        }

        void Put(Sequence sequence, string message, AbortLevel level)
        {
            var abort = sequence.Steps[_index] as Abort;
            if (abort == null)
                return;

            abort.Message = message;
            abort.Level = level;
        }
    }
}
