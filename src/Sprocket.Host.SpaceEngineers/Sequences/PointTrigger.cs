using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a trigger does: the sequence it runs or stops, or an act of its own.
    /// </summary>
    public sealed class PointTrigger : SystemEdit
    {
        readonly Automation _program;
        readonly Trigger _trigger;
        readonly Sequence _made;

        readonly Step _was;
        readonly Step _now;

        readonly List<string> _sources;
        readonly string _eventId;

        /// <summary>Has <paramref name="trigger" /> do <paramref name="does" />, carrying <paramref name="made" />, the sequence made for it, where one was.</summary>
        public PointTrigger(Automation program, Trigger trigger, Step does, Sequence made)
        {
            _program = program;
            _trigger = trigger;
            _made = made;

            _was = trigger.Does;
            _now = does;

            var moment = trigger as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment?.EventId;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.Target;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _trigger;
        }

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

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Does == _now && Holds(system, _trigger, _sources, _eventId);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Does == _was && Holds(system, _trigger, _sources, _eventId);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            // The sequence is already there the first time; a redo puts it back.
            if (_made != null && system.FindSequence(_made.Number) == null)
                _program.PutBack(system, _made, -1);

            _trigger.Does = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _trigger.Does = _was;

            if (_made != null)
                _program.RemoveSequence(_made);
        }
    }
}
