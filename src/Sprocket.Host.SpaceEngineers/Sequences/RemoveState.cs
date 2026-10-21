using System;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A state taken out of a logic system; refused while anything mentions it.</summary>
    public sealed class RemoveState : SystemEdit
    {
        readonly State _state;
        readonly int _at;
        readonly bool _held;

        /// <summary>Takes <paramref name="state" /> out of <paramref name="system" />, unless something in it mentions the state.</summary>
        public RemoveState(AutomationSystem system, State state)
        {
            _state = state;
            _at = system.States.IndexOf(state);
            _held = _at < 0 || StateMentions.Mentions(system, state.Name);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RemoveState;

        /// <summary>Changes nothing where the state is mentioned: the editor takes those away first.</summary>
        public override bool Changes => !_held;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return !system.States.Contains(_state) && !system.Takes(_state.Name);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.States.Contains(_state) && !StateMentions.Mentions(system, _state.Name);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (!_held)
                system.States.Remove(_state);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (!_held)
                system.States.Insert(Math.Min(_at, system.States.Count), _state);
        }
    }
}
