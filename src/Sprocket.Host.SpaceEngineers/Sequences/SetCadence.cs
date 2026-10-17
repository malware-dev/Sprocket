
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// How often a repeat comes round.
    /// </summary>
    /// <remarks>
    /// The trigger itself rather than the sequence it runs, because a system has several and the
    /// one being changed is the one whose row was pressed.
    /// </remarks>
    public sealed class SetCadence : SystemEdit
    {
        readonly EveryTrigger _trigger;
        readonly Duration _was;
        readonly Duration _now;

        /// <summary>The repeat <paramref name="trigger" /> made to come round every <paramref name="now" /> from every <paramref name="was" />.</summary>
        public SetCadence(EveryTrigger trigger, Duration was, Duration now)
        {
            _trigger = trigger;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Cadence;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Interval == _now && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Interval == _was && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _trigger.Interval = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _trigger.Interval = _was;
        }
    }
}
