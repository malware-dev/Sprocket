using System.Collections.Generic;
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Whether a construct may leave a part unfilled.</summary>
    public sealed class SetOptional : SystemEdit
    {
        readonly Part _part;
        readonly bool _now;

        /// <summary>Makes <paramref name="part" /> one a construct may leave unfilled where <paramref name="now" />, and required otherwise.</summary>
        public SetOptional(Part part, bool now)
        {
            _part = part;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => _now ? Change.Optional : Change.Required;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _part);
        }

        /// <inheritdoc />
        public override bool Changes => _part.Optional != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _part.Optional == _now && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _part.Optional != _now && system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _part.Optional = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _part.Optional = !_now;
        }
    }
}
