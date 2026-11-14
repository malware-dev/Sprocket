using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>Takes one or more lines out of a sequence as one edit.</summary>
    public sealed class RemoveStep : SequenceEdit
    {
        readonly ChangeKind _change;
        readonly int _index;
        readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// Takes out one or more lines as one edit.
        /// </summary>
        public RemoveStep(int index, params Step[] steps) : this(ChangeKind.RemoveStep, index, steps)
        {
        }

        /// <summary>Takes out one or more lines as one edit, called what <paramref name="change" /> says: a whole if, or a branch of one.</summary>
        public RemoveStep(ChangeKind change, int index, params Step[] steps)
        {
            _change = change;
            _index = index;
            foreach (var step in steps)
                _steps.Add(step);
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = _steps[0];
            name.Number = sequence.NumberOf(_index - 1) + 1;
            name.Count = _steps.Count;
        }

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.RemoveAt(_index);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.Insert(_index + i, _steps[i]);
        }
    }
}
