using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>The value a setting has where an installation gives none, or that it has none and every installation must give one.</summary>
    public sealed class SetSettingDefault : SystemEdit
    {
        readonly Part _part;
        readonly bool _had;
        readonly Literal _was;
        readonly bool _has;
        readonly Literal _now;

        /// <summary>Gives <paramref name="part" /> the default <paramref name="now" />, or none where <paramref name="has" /> is false.</summary>
        public SetSettingDefault(Part part, bool has, Literal now)
        {
            _part = part;
            _had = part.HasDefault;
            _was = part.Default;
            _has = has;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.SettingDefault;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _part;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _part);
        }

        /// <inheritdoc />
        public override bool Changes => _part.HasDefault != _has || _has && !StateMentions.Same(_part.Default, _now);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.FindPart(_part.Id) == _part;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            Put(_has, _now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Put(_had, _was);
        }

        void Put(bool has, Literal value)
        {
            _part.HasDefault = has;
            _part.Optional = has;
            _part.Default = has ? value : default(Literal);
        }
    }
}
