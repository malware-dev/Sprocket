using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    sealed class RemoveStep : SequenceEdit
    {
        readonly int _index;
        readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// Takes out one or more lines as one edit.
        /// </summary>
        /// <remarks>
        /// A step and the await after it go together, because they arrived together and an await left
        /// behind would wait for nothing.
        /// </remarks>
        public RemoveStep(int index, params Step[] steps)
        {
            _index = index;
            for (var i = 0; i < steps.Length; i++)
                _steps.Add(steps[i]);
        }

        public override string Label => "deleting a step";

        public override void Apply(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.RemoveAt(_index);
        }

        public override void Undo(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.Insert(_index + i, _steps[i]);
        }
    }
}
