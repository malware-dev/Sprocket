using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A type added to a logic system, together with the change that first uses it.</summary>
    public sealed class WithNewType : SystemEdit
    {
        readonly AddType _adds;
        readonly SystemEdit _then;

        /// <summary>Adds <paramref name="type" /> after the system's types, then makes <paramref name="then" />, as one edit.</summary>
        public WithNewType(AutomationSystem system, DeclaredType type, SystemEdit then)
        {
            _adds = new AddType(type, system.Types.Count);
            _then = then;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _then.Change;

        /// <inheritdoc />
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
            return _adds.Redoable(system) && _then.Redoable(system);
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
            _adds.Apply(system);
            _then.Apply(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _then.Undo(system);
            _adds.Undo(system);
        }
    }
}
