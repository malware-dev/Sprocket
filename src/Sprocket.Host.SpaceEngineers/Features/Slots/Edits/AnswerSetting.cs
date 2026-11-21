using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>The value one installation gives a setting, or the default standing again where it gives none.</summary>
    public sealed class AnswerSetting : SystemEdit
    {
        readonly SystemInstance _instance;
        readonly Part _part;
        readonly bool _had;
        readonly Literal _was;
        readonly bool _has;
        readonly Literal _now;

        /// <summary>Has <paramref name="instance" /> answer <paramref name="part" /> with <paramref name="now" />, or leave it unanswered where <paramref name="answered" /> is false.</summary>
        public AnswerSetting(SystemInstance instance, Part part, bool answered, Literal now)
        {
            _instance = instance;
            _part = part;
            _had = instance.AnswerTo(part.Id, out _was);
            _has = answered;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _has ? ChangeKind.AnswerSetting : ChangeKind.UseSettingDefault;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _part;
            name.Place = _instance;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _instance);
            Touch(screens, _part);
        }

        /// <inheritdoc />
        public override bool Changes
        {
            get
            {
                Literal now;
                var has = _instance.AnswerTo(_part.Id, out now);
                return has != _has || has && !StateMentions.Same(now, _now);
            }
        }

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

        void Put(bool answered, Literal value)
        {
            if (answered)
                _instance.Answer(_part.Id, value);
            else
                _instance.Unanswer(_part.Id);
        }
    }
}
