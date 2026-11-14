using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>What a part says about itself.</summary>
    public sealed class AnnotatePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;
        readonly ChangeKind _change;

        /// <summary>Writes <paramref name="now" /> about <paramref name="part" />, a change of the kind <paramref name="change" />.</summary>
        public AnnotatePart(Part part, string now, ChangeKind change)
        {
            _part = part;
            _was = part.Annotation;
            _now = now;
            _change = change;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _part;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _part);
        }

        /// <summary>Only what the slot says changes, and its box already says it.</summary>
        public override bool ChangesShape => false;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _part.Annotation == _now && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _part.Annotation == _was && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _part.Annotation = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _part.Annotation = _was;
        }
    }
}
