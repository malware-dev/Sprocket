using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class AddStep : SequenceEdit
    {
        readonly string _label;
        readonly int _index;
        readonly Step _step;

        public AddStep(string label, int index, Step step)
        {
            _label = label;
            _index = index;
            _step = step;
        }

        public override string Label => _label;

        public override void Apply(Sequence sequence)
        {
            sequence.Steps.Insert(_index, _step);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps.RemoveAt(_index);
        }
    }
}
