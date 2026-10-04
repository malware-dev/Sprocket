namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A slot's new name, everywhere its system names it.</summary>
    sealed class RenamePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;

        public RenamePart(Part part, string now)
        {
            _part = part;
            _was = part.Id;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.RenameSlot;

        /// <summary>Only what the slot is called changes, and its box already says it.</summary>
        public override bool ChangesShape => false;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindPart(_now) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.FindPart(_was) == _part && system.FindPart(_now) == null;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.RenamePart(_part, _now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.RenamePart(_part, _was);
        }
    }
}
