namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A change to how a system's slots stand on the installation panel: a slot or a category moved, or a category added, renamed or removed.</summary>
    public sealed class ArrangeSlots : SystemEdit
    {
        readonly ChangeKind _change;
        readonly SlotArrangement _was;
        readonly SlotArrangement _now;
        readonly object _thing;
        readonly string _renamed;

        /// <summary>
        /// Stands the slots as <paramref name="now" /> says where they stood as <paramref name="was" />, offered back as
        /// <paramref name="change" /> to <paramref name="thing" />, the slot or the name of the category moved, added, renamed or
        /// removed, and called <paramref name="renamed" /> before a rename.
        /// </summary>
        public ArrangeSlots(ChangeKind change, SlotArrangement was, SlotArrangement now, object thing, string renamed)
        {
            _thing = thing;
            _renamed = renamed;
            _change = change;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _thing;
            if (_renamed == null)
                return;

            name.Was = _renamed;
            name.Now = _thing;
        }

        /// <inheritdoc />
        public override bool Changes => !_was.Same(_now);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _now.Matches(system);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _was.Matches(system);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _now.Put(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _was.Put(system);
        }
    }
}
