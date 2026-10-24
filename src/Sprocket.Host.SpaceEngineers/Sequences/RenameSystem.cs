namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Gives a logic system another name.</summary>
    public sealed class RenameSystem : SystemEdit
    {
        readonly string _was;
        readonly string _now;

        /// <summary>Renames a logic system called <paramref name="was" /> to <paramref name="now" />.</summary>
        public RenameSystem(string was, string now)
        {
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RenameSystem;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = system;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool ChangesShape => false;

        /// <summary>A logic system is renamed whether or not it runs, so taking the name back waits for nothing either.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.Name == _now;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.Name == _was;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.Name = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.Name = _was;
        }
    }
}
