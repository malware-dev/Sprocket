namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A type's new name, on every state that holds it.</summary>
    public sealed class RenameType : SystemEdit
    {
        readonly DeclaredType _type;
        readonly string _was;
        readonly string _now;

        /// <summary>Calls <paramref name="type" /> <paramref name="now" />.</summary>
        public RenameType(DeclaredType type, string now)
        {
            _type = type;
            _was = type.Name;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RenameType;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindType(_now) == _type;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.FindType(_was) == _type && !system.Takes(_now);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            StateMentions.RenameType(system, _was, _now);
            _type.Name = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            StateMentions.RenameType(system, _now, _was);
            _type.Name = _was;
        }
    }
}
