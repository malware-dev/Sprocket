using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class MoveStep : SequenceEdit
    {
        readonly int _from;
        readonly int _to;
        readonly int _count;

        public MoveStep(int from, int to) : this(from, to, 1)
        {
        }

        /// <summary>
        /// Moves a run of lines as one.
        /// </summary>
        /// <remarks>
        /// A step and the await after it are one line as far as anybody looking at the screen is
        /// concerned, so they travel together and come back together.
        /// </remarks>
        public MoveStep(int from, int to, int count)
        {
            _from = from;
            _to = to;
            _count = count < 1 ? 1 : count;
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

        void Move(Sequence sequence, int from, int to)
        {
            var moving = new List<Step>();
            for (var i = 0; i < _count; i++)
                moving.Add(sequence.Steps[from + i]);

            for (var i = 0; i < _count; i++)
                sequence.Steps.RemoveAt(from);

            for (var i = 0; i < moving.Count; i++)
                sequence.Steps.Insert(to + i, moving[i]);
        }
    }
}
