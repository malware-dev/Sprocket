using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    sealed class ChangeStep : SequenceEdit
    {
        readonly int _index;
        readonly List<long> _wasTargets;
        readonly string _wasAction;
        readonly List<long> _nowTargets;
        readonly string _nowAction;

        public ChangeStep(int index, Step step, IList<long> targets, string actionId)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
        }

        public override string Label { get { return "changing a step"; } }

        public override bool Changes
        {
            get { return _wasAction != _nowAction || !Same(_wasTargets, _nowTargets); }
        }

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasTargets, _wasAction);
        }

        void Put(Sequence sequence, IList<long> targets, string action)
        {
            var step = sequence.Steps[_index];
            Refill(step.Targets, targets);
            step.ActionId = action;
        }
    }
}
