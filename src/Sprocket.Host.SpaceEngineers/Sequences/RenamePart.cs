namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A slot's new name, everywhere its system names it.</summary>
    public sealed class RenamePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;

        /// <summary>Calls <paramref name="part" /> <paramref name="now" />.</summary>
        public RenamePart(Part part, string now)
        {
            _part = part;
            _was = part.Id;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.RenameSlot;

        /// <summary>The configuration panel lists its slots in order of their names, so a slot renamed may move.</summary>
        public override bool ChangesShape => true;

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
