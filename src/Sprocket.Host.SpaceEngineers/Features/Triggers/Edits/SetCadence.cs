using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers.Edits
{
    /// <summary>
    /// How often a repeat comes round: a fixed rate, a setting, or an expression.
    /// </summary>
    public sealed class SetCadence : SystemEdit
    {
        readonly EveryTrigger _trigger;
        readonly Duration _was;
        readonly string _wasSetting;
        readonly Expression _wasExpression;
        readonly Duration _now;
        readonly string _nowSetting;
        readonly Expression _nowExpression;

        /// <summary>
        /// The repeat <paramref name="trigger" /> made to come round every <paramref name="now" />, or at the setting <paramref name="nowSetting" />
        /// where it is not empty, from every <paramref name="was" />, or the setting <paramref name="wasSetting" /> where it took one,
        /// or the expression <paramref name="wasExpression" /> where it took that; or as often as <paramref name="nowExpression" /> works out to, where it is given.
        /// </summary>
        public SetCadence(EveryTrigger trigger, Duration was, string wasSetting, Duration now, string nowSetting = "", Expression wasExpression = null,
            Expression nowExpression = null)
        {
            _nowExpression = nowExpression;
            _trigger = trigger;
            _was = was;
            _wasSetting = wasSetting;
            _wasExpression = wasExpression;
            _now = now;
            _nowSetting = nowSetting ?? "";
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Cadence;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _trigger;
            name.Was = _wasExpression ?? (!string.IsNullOrEmpty(_wasSetting) ? (object)_wasSetting : _was);
            name.Now = _nowExpression ?? (!string.IsNullOrEmpty(_nowSetting) ? (object)_nowSetting : _now);
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now || _wasSetting != _nowSetting || _wasExpression != _nowExpression;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Interval == _now && _trigger.IntervalSetting == _nowSetting && _trigger.IntervalExpression == _nowExpression && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Interval == _was && _trigger.IntervalSetting == _wasSetting && _trigger.IntervalExpression == _wasExpression && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _trigger.Interval = _now;
            _trigger.IntervalSetting = _nowSetting;
            _trigger.IntervalExpression = _nowExpression;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _trigger.Interval = _was;
            _trigger.IntervalSetting = _wasSetting;
            _trigger.IntervalExpression = _wasExpression;
        }
    }
}
