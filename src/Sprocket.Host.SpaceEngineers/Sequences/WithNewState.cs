using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A state added to a logic system, with the type made for it where one was, together with the change that first uses it, as one edit.</summary>
    public sealed class WithNewState : SystemEdit
    {
        readonly AddType _types;
        readonly AddState _adds;
        readonly SystemEdit _then;

        /// <summary>
        /// Adds <paramref name="type" /> after the system's types where it is not null, and <paramref name="state" /> after its
        /// states, then makes <paramref name="then" />, as one edit.
        /// </summary>
        public WithNewState(AutomationSystem system, State state, DeclaredType type, SystemEdit then)
        {
            _types = type == null ? null : new AddType(type, system.Types.Count);
            _adds = new AddState(state, system.States.Count);
            _then = then;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _then.Change;

        /// <summary>What the change made with the new state was made to.</summary>
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            _then.Name(system, done, name);
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _then.Undoable(system);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return (_types == null || _types.Redoable(system)) && _adds.Redoable(system) && _then.Redoable(system);
        }

        /// <inheritdoc />
        public override bool Held(AutomationSystem system, bool back)
        {
            return _then.Held(system, back);
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            _then.Touches(system, screens);
        }

        /// <inheritdoc />
        public override void Reassert(AutomationSystem system)
        {
            _then.Reassert(system);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _types?.Apply(system);
            _adds.Apply(system);
            _then.Apply(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _then.Undo(system);
            _adds.Undo(system);
            _types?.Undo(system);
        }
    }
}
