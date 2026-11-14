using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    /// <remarks>Also puts in or takes out the await after the step, to match what it now does.</remarks>
    public sealed class ChangeStep : SequenceEdit
    {
        readonly int _index;
        readonly List<string> _wasTargets;
        readonly string _wasAction;
        readonly ReachKind _wasForm;
        readonly string _wasProvider;
        readonly List<Literal> _wasValues;
        readonly List<string> _nowTargets;
        readonly string _nowAction;
        readonly ReachKind _nowForm;
        readonly string _nowProvider;
        readonly List<Literal> _nowValues;
        readonly bool _awaited;

        Await _added;
        Await _dropped;

        /// <summary>
        /// A change to <paramref name="step" />, at <paramref name="index" />, followed by an await where
        /// <paramref name="awaited" /> says what it is changed to takes time to finish.
        /// </summary>
        public ChangeStep(int index, Act step, IList<string> targets, string actionId, ReachKind form, string provider,
            IList<Literal> arguments, bool awaited)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _wasForm = step.Form;
            _wasProvider = step.Provider;
            _nowProvider = provider;
            _wasValues = CopyOf(step.Arguments);
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
            _nowForm = form;
            _nowValues = CopyOf(arguments);
            _awaited = awaited;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Step;

        /// <inheritdoc />
        public override void Name(Sequence sequence, bool done, ChangeName name)
        {
            name.Thing = sequence.Steps[_index];
            name.Number = sequence.NumberOf(_index);
        }

        /// <inheritdoc />
        public override bool Changes =>
            _wasAction != _nowAction || _wasForm != _nowForm || _wasProvider != _nowProvider
            || !Same(_wasTargets, _nowTargets) || !Same(_wasValues, _nowValues);

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction, _nowForm, _nowProvider, _nowValues);

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

            Put(sequence, _wasTargets, _wasAction, _wasForm, _wasProvider, _wasValues);
        }

        void Put(Sequence sequence, IList<string> targets, string action, ReachKind form, string provider,
            IList<Literal> arguments)
        {
            var step = sequence.Steps[_index] as Act;
            if (step == null)
                return;

            Refill(step.Targets, targets);
            step.ActionId = action;
            step.Form = form;
            step.Provider = provider;
            Refill(step.Arguments, arguments);
        }
    }
}
