
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Switches a step off, or back on, with the wait for it to finish that goes with it.</summary>
    sealed class SwitchStep : SequenceEdit
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

        public override Change Change => _off ? Change.PauseStep : Change.ResumeStep;

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _off);
        }

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
