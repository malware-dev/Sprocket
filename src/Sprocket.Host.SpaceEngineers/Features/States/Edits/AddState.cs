using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>A state added to a logic system, at a place among its states.</summary>
    public sealed class AddState : SystemEdit
    {
        readonly State _state;
        readonly int _at;

        /// <summary>Adds <paramref name="state" /> at <paramref name="at" /> among the system's states.</summary>
        public AddState(State state, int at)
        {
            _state = state;
            _at = at;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.AddState;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _state;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.States.Contains(_state) && !StateMentions.Mentions(system, _state.Name);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return !system.States.Contains(_state) && system.FindState(_state.Name) == null;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.States.Insert(Math.Min(_at, system.States.Count), _state);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.States.Remove(_state);
        }
    }
}
