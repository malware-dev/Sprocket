using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

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
        readonly Form _wasForm;
        readonly List<Literal> _wasValues;
        readonly List<string> _nowTargets;
        readonly string _nowAction;
        readonly Form _nowForm;
        readonly List<Literal> _nowValues;

        public ChangeStep(int index, Act step, IList<string> targets, string actionId, Form form,
            IList<Literal> values)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _wasForm = step.Form;
            _wasValues = CopyOf(step.Values);
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
            _nowForm = form;
            _nowValues = CopyOf(values);
        }

        public override string Label => "changing a step";

        public override bool Changes =>
            _wasAction != _nowAction || _wasForm != _nowForm || !Same(_wasTargets, _nowTargets)
            || !Same(_wasValues, _nowValues);

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction, _nowForm, _nowValues);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasTargets, _wasAction, _wasForm, _wasValues);
        }

        void Put(Sequence sequence, IList<string> targets, string action, Form form,
            IList<Literal> values)
        {
            var step = sequence.Steps[_index] as Act;
            if (step == null)
                return;

            Refill(step.Targets, targets);
            step.ActionId = action;
            step.Form = form;
            Refill(step.Values, values);
        }
    }
}
