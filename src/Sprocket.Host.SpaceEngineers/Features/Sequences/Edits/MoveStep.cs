using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>Moves one or more lines of a sequence to another place in it, as one edit.</summary>
    public sealed class MoveStep : SequenceEdit
    {
        readonly int _from;
        readonly int _to;
        readonly int _count;

        /// <summary>Moves the line at <paramref name="from" /> to <paramref name="to" />.</summary>
        public MoveStep(int from, int to) : this(from, to, 1)
        {
        }

        /// <summary>
        /// Moves a run of lines as one.
        /// </summary>
        public MoveStep(int from, int to, int count)
        {
            _from = from;
            _to = to;
            _count = count < 1 ? 1 : count;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.MoveStep;

        /// <summary>The lines moved, with the numbers the first is shown with before and after the move.</summary>
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            var at = done ? _to : _from;
            name.Thing = sequence.Steps[at];
            name.Was = NumberAt(sequence, at, _from);
            name.Now = NumberAt(sequence, at, _to);
            name.Number = (int)name.Was;
            name.Count = _count;
        }

        /// <summary>The number the first moved line is shown with when the lines are at <paramref name="to" />, as they stand at <paramref name="at" /> now.</summary>
        int NumberAt(Sequence sequence, int at, int to)
        {
            var number = 1;
            var placed = 0;
            for (var i = 0; i < sequence.Steps.Count && placed < to; i++)
            {
                if (i >= at && i < at + _count)
                    continue;

                placed++;
                if (!(sequence.Steps[i] is EndIf))
                    number++;
            }

            return number;
        }

        /// <inheritdoc />
        public override bool Changes => _from != _to;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Move(sequence, _from, _to);
        }

        /// <inheritdoc />
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
