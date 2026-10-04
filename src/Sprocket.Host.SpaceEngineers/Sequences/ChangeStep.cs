using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    /// <remarks>
    /// The await after the step follows what it now does: one is put in where it takes time to finish,
    /// and the one there is taken out where it does not.
    /// </remarks>
    public sealed class ChangeStep : SequenceEdit
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
        readonly bool _awaited;

        Await _added;
        Await _dropped;

        /// <summary>
        /// A change to <paramref name="step" />, at <paramref name="index" />, followed by an await where
        /// <paramref name="awaited" /> says what it is changed to takes time to finish.
        /// </summary>
        public ChangeStep(int index, Act step, IList<string> targets, string actionId, Form form,
            IList<Literal> values, bool awaited)
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
            _awaited = awaited;
        }

        /// <inheritdoc />
        public override Change Change => Change.Step;

        /// <inheritdoc />
        public override bool Changes =>
            _wasAction != _nowAction || _wasForm != _nowForm || !Same(_wasTargets, _nowTargets)
            || !Same(_wasValues, _nowValues);

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction, _nowForm, _nowValues);

            _added = null;
            _dropped = null;
            var after = _index + 1 < sequence.Steps.Count ? sequence.Steps[_index + 1] as Await : null;
            if (_awaited && after == null)
            {
                _added = new Await();
                sequence.Steps.Insert(_index + 1, _added);
            }
            else if (!_awaited && after != null)
            {
                _dropped = after;
                sequence.Steps.RemoveAt(_index + 1);
            }
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            if (_added != null)
                sequence.Steps.RemoveAt(_index + 1);
            else if (_dropped != null)
                sequence.Steps.Insert(_index + 1, _dropped);

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
