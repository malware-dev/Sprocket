using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>Takes out lines that need not stand together as one edit: an if's own lines, leaving the steps in it where they are.</summary>
    public sealed class UnwrapIf : SequenceEdit
    {
        readonly ChangeKind _change;
        readonly List<int> _lines = new List<int>();
        readonly List<Step> _steps = new List<Step>();

        /// <summary>Takes out the line at each of <paramref name="lines" />, which are in order.</summary>
        public UnwrapIf(ChangeKind change, IList<int> lines, IList<Step> steps)
        {
            _change = change;
            for (var i = 0; i < lines.Count; i++)
            {
                _lines.Add(lines[i]);
                _steps.Add(steps[i]);
            }
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = _steps[0];
            name.Number = sequence.NumberOf(_lines[0] - 1) + 1;
        }

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            for (var i = _lines.Count - 1; i >= 0; i--)
                sequence.Steps.RemoveAt(_lines[i]);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            for (var i = 0; i < _lines.Count; i++)
                sequence.Steps.Insert(_lines[i], _steps[i]);
        }
    }
}
