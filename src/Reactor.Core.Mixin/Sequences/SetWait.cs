using System.Collections.Generic;

namespace Reactor.Sequences
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

        public override string Label => "changing a wait";

        public override bool Changes => _was != _now;

        public override void Apply(Sequence sequence)
        {
            sequence.Steps[_index].Gate = new AfterGate(_now);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps[_index].Gate = new AfterGate(_was);
        }
    }
}
