namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// How often a repeat comes round.
    /// </summary>
    /// <remarks>
    /// The trigger itself rather than the sequence it runs, because a system has several and the
    /// one being changed is the one whose row was pressed.
    /// </remarks>
    sealed class SetCadence : SystemEdit
    {
        readonly EveryTrigger _trigger;
        readonly int _was;
        readonly int _now;

        public SetCadence(EveryTrigger trigger, int was, int now)
        {
            _trigger = trigger;
            _was = was;
            _now = now;
        }

        public override string Label => "changing how often it runs";

        public override bool Changes => _was != _now;

        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.IntervalTicks == _now && Holds(system, _trigger, null, null);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.IntervalTicks == _was && Holds(system, _trigger, null, null);
        }

        public override void Apply(AutomationSystem system)
        {
            _trigger.IntervalTicks = _now;
        }

        public override void Undo(AutomationSystem system)
        {
            _trigger.IntervalTicks = _was;
        }
    }
}
