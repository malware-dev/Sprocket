using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A signal slot's short name, the few words its buttons and toolbar slots say.</summary>
    public sealed class ShortenPart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;

        /// <summary>Gives <paramref name="part" /> the short name <paramref name="now" />, or none where it is empty.</summary>
        public ShortenPart(Part part, string now)
        {
            _part = part;
            _was = part.Short;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.ShortenSlot;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _part);
        }

        /// <summary>Only the short name changes, and its box already says it.</summary>
        public override bool ChangesShape => false;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _part.Short == _now && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _part.Short == _was && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _part.Short = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _part.Short = _was;
        }
    }
}
