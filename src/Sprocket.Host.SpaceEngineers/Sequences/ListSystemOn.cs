namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Keeps a logic system to one block's list, or lets it list on every block again.</summary>
    public sealed class ListSystemOn : SystemEdit
    {
        readonly long _was;
        readonly long _now;

        /// <summary>Changes the block a logic system lists on from <paramref name="was" /> to <paramref name="now" />, entity ids with 0 for every block.</summary>
        public ListSystemOn(long was, long now)
        {
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.ListedOn;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = system;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.ListedOn == _now;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.ListedOn == _was;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.ListedOn = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.ListedOn = _was;
        }
    }
}
