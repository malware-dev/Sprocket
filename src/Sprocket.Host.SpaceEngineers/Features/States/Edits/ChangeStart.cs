using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>
    /// A state's new starting value, which may make it hold another type; refused where it would change the type of a
    /// state something mentions.
    /// </summary>
    public sealed class ChangeStart : SystemEdit
    {
        readonly State _state;
        readonly Literal _was;
        readonly string _wasType;
        readonly Literal _now;
        readonly string _nowType;
        readonly bool _held;

        /// <summary>Starts <paramref name="state" /> at <paramref name="now" />, a choice of <paramref name="type" />, or a flag where that is empty.</summary>
        public ChangeStart(AutomationSystem system, State state, Literal now, string type)
        {
            _state = state;
            _was = state.Start;
            _wasType = state.Type;
            _now = now;
            _nowType = type ?? "";
            _held = _wasType != _nowType && StateMentions.Mentions(system, state.Name);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.ChangeStart;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _state;
            name.Was = _was;
            name.Now = _now;
        }

        /// <summary>Changes nothing where the value is the same, or a type would change under what mentions the state.</summary>
        public override bool Changes => !_held && (!StateMentions.Same(_was, _now) || _wasType != _nowType);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return StateMentions.Same(_state.Start, _now) && _state.Type == _nowType;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return StateMentions.Same(_state.Start, _was) && _state.Type == _wasType;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_held)
                return;

            _state.Start = _now;
            _state.Type = _nowType;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (_held)
                return;

            _state.Start = _was;
            _state.Type = _wasType;
        }
    }
}
