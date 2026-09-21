using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    sealed class ChangeStep : SequenceEdit
    {
        readonly int _index;
        readonly List<string> _wasTargets;
        readonly string _wasAction;
        readonly bool _wasSets;
        readonly List<string> _nowTargets;
        readonly string _nowAction;
        readonly bool _nowSets;

        public ChangeStep(int index, Act step, IList<string> targets, string actionId, bool sets)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _wasSets = step.Sets;
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
            _nowSets = sets;
        }

        public override string Label => "changing a step";

        public override bool Changes =>
            _wasAction != _nowAction || _wasSets != _nowSets || !Same(_wasTargets, _nowTargets);

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction, _nowSets);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasTargets, _wasAction, _wasSets);
        }

        void Put(Sequence sequence, IList<string> targets, string action, bool sets)
        {
            var step = sequence.Steps[_index] as Act;
            if (step == null)
                return;

            Refill(step.Targets, targets);
            step.ActionId = action;
            step.Sets = sets;
        }
    }
}
