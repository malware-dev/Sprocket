using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>
    /// A change to how a list of a system's things stands on its screen, such as its slots or its sequences: one of them or a
    /// category moved, or a category added, renamed or removed.
    /// </summary>
    public sealed class Rearrange : SystemEdit
    {
        readonly ChangeKind _change;
        readonly Func<AutomationSystem, ICategorizedList> _list;
        readonly Arrangement _was;
        readonly Arrangement _now;
        readonly object _thing;
        readonly string _renamed;

        /// <summary>
        /// Stands the system's list <paramref name="list" /> finds as <paramref name="now" /> says where it stood as
        /// <paramref name="was" />, offered back as <paramref name="change" /> to <paramref name="thing" />, the one or the name of
        /// the category moved, added, renamed or removed, and called <paramref name="renamed" /> before a rename.
        /// </summary>
        public Rearrange(ChangeKind change, Func<AutomationSystem, ICategorizedList> list, Arrangement was, Arrangement now, object thing,
            string renamed)
        {
            _change = change;
            _list = list;
            _was = was;
            _now = now;
            _thing = thing;
            _renamed = renamed;
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
            return _now.Matches(_list(system));
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _was.Matches(_list(system));
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _now.Put(_list(system));
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _was.Put(_list(system));
        }
    }
}
