using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>The type of block a slot takes, changed on the slot's own page.</summary>
    public sealed class SetExpects : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;

        /// <summary>Has <paramref name="part" /> take blocks of the type <paramref name="now" />, written as a slot writes it, or any block for empty.</summary>
        public SetExpects(Part part, string now)
        {
            _part = part;
            _was = part.Expects;
            _now = now ?? "";
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.SlotType;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _part;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _part);
            Touch(screens, system);
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _part.Expects == _now && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _part.Expects == _was && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _part.Expects = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _part.Expects = _was;
        }
    }
}
