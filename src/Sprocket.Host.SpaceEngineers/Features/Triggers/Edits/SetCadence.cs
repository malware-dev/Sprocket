using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers.Edits
{
    /// <summary>
    /// How often a repeat comes round: a fixed rate, in place of a setting it took.
    /// </summary>
    public sealed class SetCadence : SystemEdit
    {
        readonly EveryTrigger _trigger;
        readonly Duration _was;
        readonly string _wasSetting;
        readonly Duration _now;

        /// <summary>The repeat <paramref name="trigger" /> made to come round every <paramref name="now" /> from every <paramref name="was" />, or from the setting <paramref name="wasSetting" /> where it took one.</summary>
        public SetCadence(EveryTrigger trigger, Duration was, string wasSetting, Duration now)
        {
            _trigger = trigger;
            _was = was;
            _wasSetting = wasSetting;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Cadence;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _trigger;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now || _wasSetting.Length > 0;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Interval == _now && _trigger.IntervalSetting.Length == 0 && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Interval == _was && _trigger.IntervalSetting == _wasSetting && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _trigger.Interval = _now;
            _trigger.IntervalSetting = "";
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _trigger.Interval = _was;
            _trigger.IntervalSetting = _wasSetting;
        }
    }
}
