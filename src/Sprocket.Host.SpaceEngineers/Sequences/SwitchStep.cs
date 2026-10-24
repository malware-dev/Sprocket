
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Switches steps off, or back on.</summary>
    public sealed class SwitchStep : SequenceEdit
    {
        readonly int _index;
        readonly int _count;
        readonly bool _off;

        /// <summary>Switches the <paramref name="count" /> steps from <paramref name="index" /> off where <paramref name="off" /> says so, and on otherwise.</summary>
        public SwitchStep(int index, int count, bool off)
        {
            _index = index;
            _count = count;
            _off = off;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _off ? ChangeKind.PauseStep : ChangeKind.ResumeStep;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
            name.Count = _count;
        }

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _off);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, !_off);
        }

        void Put(Sequence sequence, bool off)
        {
            for (var i = _index; i < _index + _count && i < sequence.Steps.Count; i++)
                sequence.Steps[i].IsOff = off;
        }
    }
}
