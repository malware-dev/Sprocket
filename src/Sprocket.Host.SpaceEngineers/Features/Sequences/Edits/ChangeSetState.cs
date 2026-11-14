using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences.Edits
{
    /// <summary>What a step setting one of the logic system's states sets, and to what, changed.</summary>
    public sealed class ChangeSetState : SequenceEdit
    {
        readonly int _index;
        readonly SetState _was;
        readonly SetState _now;

        /// <summary>Changes the step at <paramref name="index" /> from setting what <paramref name="was" /> sets to setting what <paramref name="now" /> does.</summary>
        public ChangeSetState(int index, SetState was, SetState now)
        {
            _index = index;
            _was = new SetState { State = was.State, Value = was.Value, From = was.From };
            _now = now;
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
        public override bool Changes => _was.State != _now.State || _was.From != _now.From || !StateMentions.Same(_was.Value, _now.Value);

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);
        }

        void Put(Sequence sequence, SetState from)
        {
            var set = sequence.Steps[_index] as SetState;
            if (set == null)
                return;

            set.State = from.State;
            set.Value = from.Value;
            set.From = from.From;
        }
    }
}
