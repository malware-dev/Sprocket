using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class Rename : SequenceEdit
    {
        readonly string _was;
        readonly string _now;

        public Rename(string was, string now)
        {
            _was = was;
            _now = now;
        }

        public override string Label { get { return "renaming"; } }

        public override bool ChangesShape { get { return false; } }

        public override bool Changes { get { return _was != _now; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Name = _now;
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Name = _was;
        }
    }
}
