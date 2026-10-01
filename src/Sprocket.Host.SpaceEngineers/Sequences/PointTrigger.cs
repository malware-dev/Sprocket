using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a trigger does: the sequence it runs or stops, or an act of its own.
    /// </summary>
    /// <remarks>
    /// The trigger itself is kept rather than replaced, because what starts it has not changed - and
    /// the toolbar slot the construct holds names that trigger. Rebuilding it would claim the slot
    /// again for something that is, to the player, the same button.
    /// <para>
    /// Answering "run a new sequence" makes one, so this carries it, and undoing takes it away again.
    /// </para>
    /// </remarks>
    sealed class PointTrigger : SystemEdit
    {
        readonly Automation _program;
        readonly Trigger _trigger;
        readonly Sequence _made;

        readonly Step _was;
        readonly Step _now;

        readonly List<string> _sources;
        readonly string _eventId;

        public PointTrigger(Automation program, Trigger trigger, Step does, Sequence made)
        {
            _program = program;
            _trigger = trigger;
            _made = made;

            _was = trigger.Does;
            _now = does;

            var moment = trigger as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment == null ? null : moment.EventId;
        }

        public override Change Change => Change.Target;

        /// <summary>Whether it does something else: a sequence run the same way again is no change.</summary>
        public override bool Changes
        {
            get
            {
                var was = _was as Run;
                var now = _now as Run;
                if (was != null && now != null)
                    return was.Number != now.Number || was.Stops != now.Stops;

                return _was != _now;
            }
        }

        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Does == _now && Holds(system, _trigger, _sources, _eventId);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Does == _was && Holds(system, _trigger, _sources, _eventId);
        }

        public override void Apply(AutomationSystem system)
        {
            // Already there the first time, and put back by this on the way forward again.
            if (_made != null && system.FindSequence(_made.Number) == null)
                _program.PutBack(_made);

            _trigger.Does = _now;
        }

        public override void Undo(AutomationSystem system)
        {
            _trigger.Does = _was;

            if (_made != null)
                _program.RemoveSequence(_made);
        }
    }
}
