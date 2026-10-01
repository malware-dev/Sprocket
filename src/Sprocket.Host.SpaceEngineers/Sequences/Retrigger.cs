
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What starts a trigger and what it does, changed by one answer.
    /// </summary>
    /// <remarks>
    /// One edit rather than two, because the picker that changes what starts a trigger says what it
    /// does beside it, and one Done is one thing to walk back.
    /// <para>
    /// The second is about the trigger the first puts in place, so it can be walked forward only once
    /// the first has been, and whether the pair can be is the first one's to say. Nothing outside the
    /// editor can reach a trigger that is not in the system.
    /// </para>
    /// </remarks>
    sealed class Retrigger : SystemEdit
    {
        readonly SystemEdit _start;
        readonly SystemEdit _does;

        public Retrigger(SystemEdit start, SystemEdit does)
        {
            _start = start;
            _does = does;
        }

        public override Change Change => Change.Trigger;

        public override bool Changes => _start.Changes || _does.Changes;

        public override bool Undoable(AutomationSystem system)
        {
            return _does.Undoable(system) && _start.Undoable(system);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _start.Redoable(system);
        }

        public override void Apply(AutomationSystem system)
        {
            _start.Apply(system);
            _does.Apply(system);
        }

        public override void Undo(AutomationSystem system)
        {
            _does.Undo(system);
            _start.Undo(system);
        }
    }
}
