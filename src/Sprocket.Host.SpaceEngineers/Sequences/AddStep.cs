using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Adds one or more lines to a sequence as one edit.</summary>
    public sealed class AddStep : SequenceEdit
    {
        readonly ChangeKind _change;
        readonly int _index;
        readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// One or more lines added as one edit.
        /// </summary>
        public AddStep(ChangeKind change, int index, params Step[] steps)
        {
            _change = change;
            _index = index;
            foreach (var step in steps)
                _steps.Add(step);
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.Insert(_index + i, _steps[i]);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
                sequence.Steps.RemoveAt(_index + i);
        }
    }
}
