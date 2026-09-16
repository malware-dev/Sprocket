using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    sealed class ChangeStep : SequenceEdit
    {
        readonly int _index;
        readonly List<string> _wasTargets;
        readonly string _wasAction;
        readonly List<string> _nowTargets;
        readonly string _nowAction;

        public ChangeStep(int index, Act step, IList<string> targets, string actionId)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
        }

        public override string Label => "changing a step";

        public override bool Changes => _wasAction != _nowAction || !Same(_wasTargets, _nowTargets);

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasTargets, _wasAction);
        }

        void Put(Sequence sequence, IList<string> targets, string action)
        {
            var step = sequence.Steps[_index] as Act;
            if (step == null)
                return;

            Refill(step.Targets, targets);
            step.ActionId = action;
        }
    }
}
