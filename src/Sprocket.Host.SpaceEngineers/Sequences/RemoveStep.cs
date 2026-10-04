using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    sealed class RemoveStep : SequenceEdit
    {
        readonly Change _change;
        readonly int _index;
        readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// Takes out one or more lines as one edit.
        /// </summary>
        /// <remarks>
        /// A step and the await after it go together, because they arrived together and an await left
        /// behind would wait for nothing.
        /// </remarks>
        public RemoveStep(int index, params Step[] steps) : this(Change.RemoveStep, index, steps)
        {
        }

        /// <summary>The same, called what <paramref name="change" /> says: a whole if, or a branch of one.</summary>
        public RemoveStep(Change change, int index, params Step[] steps)
        {
            _change = change;
            _index = index;
            for (var i = 0; i < steps.Length; i++)
                _steps.Add(steps[i]);
        }

        /// <inheritdoc />
        public override Change Change => _change;

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
