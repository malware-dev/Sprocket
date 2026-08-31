using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class MoveStep : SequenceEdit
    {
        readonly int _from;
        readonly int _to;

        public MoveStep(int from, int to)
        {
            _from = from;
            _to = to;
        }

        public override string Label => "moving a step";

        public override bool Changes => _from != _to;

        public override void Apply(Sequence sequence)
        {
            Move(sequence, _from, _to);
        }

        public override void Undo(Sequence sequence)
        {
            Move(sequence, _to, _from);
        }

        static void Move(Sequence sequence, int from, int to)
        {
            var step = sequence.Steps[from];
            sequence.Steps.RemoveAt(from);
            sequence.Steps.Insert(to, step);
        }
    }
}
