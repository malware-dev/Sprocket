using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>
    /// The value a state has now in one installation, changed by hand while that installation or its logic system is switched
    /// off. Nothing watching the state reacts, then or later.
    /// </summary>
    public sealed class ChangeNow : SystemEdit
    {
        readonly SystemInstance _instance;
        readonly State _state;
        readonly Literal _was;
        readonly Literal _now;
        readonly Func<Literal> _read;
        readonly Action<Literal> _write;

        /// <summary>
        /// Gives <paramref name="state" /> of <paramref name="instance" /> the value <paramref name="now" /> in place of what
        /// <paramref name="read" /> finds it holding, by <paramref name="write" />.
        /// </summary>
        public ChangeNow(SystemInstance instance, State state, Literal now, Func<Literal> read, Action<Literal> write)
        {
            _instance = instance;
            _state = state;
            _was = read();
            _now = now;
            _read = read;
            _write = write;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.ChangeNow;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _state;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => !StateMentions.Same(_was, _now);

        /// <summary>Waits while the installation and its logic system are both switched on, since a value is changed by hand only while one is off.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return system.Enabled && _instance.Enabled;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return Has(system, _instance) && system.States.Contains(_state) && StateMentions.Same(_read(), _now);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Has(system, _instance) && system.States.Contains(_state) && StateMentions.Same(_read(), _was);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _write(_now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _write(_was);
        }
    }
}
