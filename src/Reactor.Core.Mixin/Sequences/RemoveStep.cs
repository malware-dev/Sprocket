using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class RemoveStep : SequenceEdit
    {
        readonly int _index;
        readonly Step _step;

        public RemoveStep(int index, Step step)
        {
            _index = index;
            _step = step;
        }

        public override string Label { get { return "deleting a step"; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Steps.RemoveAt(_index);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps.Insert(_index, _step);
        }
    }
}
