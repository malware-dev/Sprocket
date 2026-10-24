
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What starts a trigger and what it does, changed by one answer.
    /// </summary>
    public sealed class Retrigger : SystemEdit
    {
        readonly SystemEdit _start;
        readonly SystemEdit _does;

        /// <summary>Changes what starts a trigger by <paramref name="start" /> and what it does by <paramref name="does" />, as one edit.</summary>
        public Retrigger(SystemEdit start, SystemEdit does)
        {
            _start = start;
            _does = does;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Trigger;

        /// <summary>What the trigger changed is: its start's change names it, or what it does where that names nothing.</summary>
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            _start.Name(system, done, name);
            if (name.Thing == null)
                _does.Name(system, done, name);
        }

        /// <inheritdoc />
        public override bool Changes => _start.Changes || _does.Changes;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _does.Undoable(system) && _start.Undoable(system);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _start.Redoable(system);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _start.Apply(system);
            _does.Apply(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _does.Undo(system);
            _start.Undo(system);
        }
    }
}
