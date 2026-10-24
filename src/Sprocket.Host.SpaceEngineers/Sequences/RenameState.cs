namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A state's new name, everywhere its system names it.</summary>
    public sealed class RenameState : SystemEdit
    {
        readonly State _state;
        readonly string _was;
        readonly string _now;

        /// <summary>Calls <paramref name="state" /> <paramref name="now" />.</summary>
        public RenameState(State state, string now)
        {
            _state = state;
            _was = state.Name;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RenameState;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _state;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindState(_now) == _state;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.FindState(_was) == _state && !system.Takes(_now);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            StateMentions.RenameState(system, _was, _now);
            _state.Name = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            StateMentions.RenameState(system, _now, _was);
            _state.Name = _was;
        }
    }
}
