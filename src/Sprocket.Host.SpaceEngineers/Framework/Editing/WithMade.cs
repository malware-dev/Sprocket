using Sprocket.Host.SpaceEngineers.Features.States.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>Something added to a logic system, together with the change that first uses it, as one edit.</summary>
    public sealed class WithMade : SystemEdit
    {
        readonly SystemEdit _made;
        readonly SystemEdit _then;

        /// <summary>Makes <paramref name="made" />, then <paramref name="then" />, which names and decides the edit.</summary>
        public WithMade(SystemEdit made, SystemEdit then)
        {
            _made = made;
            _then = then;
        }

        /// <summary>
        /// Adds <paramref name="type" /> after the system's types where it is not null, and <paramref name="state" /> after its
        /// states, then makes <paramref name="then" />, as one edit.
        /// </summary>
        public static SystemEdit State(AutomationSystem system, State state, DeclaredType type, SystemEdit then)
        {
            var added = new WithMade(new AddState(state, system.States.Count), then);
            return type == null ? added : new WithMade(new AddType(type, system.Types.Count), added);
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
            return _made.Redoable(system) && _then.Redoable(system);
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
            _made.Apply(system);
            _then.Apply(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _then.Undo(system);
            _made.Undo(system);
        }
    }
}
