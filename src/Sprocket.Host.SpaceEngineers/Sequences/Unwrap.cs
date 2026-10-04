using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Takes out lines that need not stand together as one edit: an if's own lines, leaving the steps in it where they are.</summary>
    sealed class Unwrap : SequenceEdit
    {
        readonly Change _change;
        readonly List<int> _lines = new List<int>();
        readonly List<Step> _steps = new List<Step>();

        /// <summary>Takes out the line at each of <paramref name="lines" />, which are in order.</summary>
        public Unwrap(Change change, IList<int> lines, IList<Step> steps)
        {
            _change = change;
            for (var i = 0; i < lines.Count; i++)
            {
                _lines.Add(lines[i]);
                _steps.Add(steps[i]);
            }
        }

        public override Change Change => _change;

        public override void Apply(Sequence sequence)
        {
            for (var i = _lines.Count - 1; i >= 0; i--)
                sequence.Steps.RemoveAt(_lines[i]);
        }

        public override void Undo(Sequence sequence)
        {
            for (var i = 0; i < _lines.Count; i++)
                sequence.Steps.Insert(_lines[i], _steps[i]);
        }
    }
}
