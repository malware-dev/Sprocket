using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Takes a trigger out of a system, and the sequence it was the only reason for.
    /// </summary>
    /// <remarks>
    /// A sequence nobody named exists only to be the one thing its trigger does, so it goes with it.
    /// One that is named, or that something else still starts, stays behind: it is a thing in its own
    /// right and somebody may be about to point another trigger at it.
    /// <para>
    /// That is also why this is in the system's history rather than asking first. Deleting a trigger
    /// can take steps nobody has seen since they were written, and a question in front of every
    /// deletion teaches people to press through questions. The way back is the answer to both.
    /// </para>
    /// <para>
    /// Both come back where they were. A trigger put back on the end would reorder the list somebody
    /// is looking at, which reads as a second change rather than as the first one being undone.
    /// </para>
    /// </remarks>
    sealed class ForgetTrigger : SystemEdit
    {
        readonly Automation _program;
        readonly Trigger _going;
        readonly Sequence _only;

        int _at;

        readonly List<string> _sources;
        readonly string _eventId;

        public ForgetTrigger(Automation program, Trigger going, Sequence only)
        {
            _program = program;
            _going = going;
            _only = only;

            var moment = going as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment == null ? null : moment.EventId;
        }

        public override string Label => "deleting a trigger";

        /// <summary>
        /// Only while nothing has taken the place of what went.
        /// </summary>
        /// <remarks>
        /// A number is the name everything else knows a sequence by, so one that has since been
        /// handed out again is a different sequence wearing it - and putting the old one back would
        /// answer to whatever now points at that number.
        /// </remarks>
        public override bool Undoable(AutomationSystem system)
        {
            if (!Gone(system, _going))
                return false;

            return _only == null || system.FindSequence(_only.Number) == null;
        }

        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system, _going, _sources, _eventId);
        }

        public override void Apply(AutomationSystem system)
        {
            _at = system.IndexOfTrigger(_going);
            system.ReplaceTrigger(_going, null);

            if (_only != null)
                _program.RemoveSequence(_only);
        }

        public override void Undo(AutomationSystem system)
        {
            if (_only != null)
                _program.PutBack(_only);

            system.PutTriggerBack(_at, _going);
        }
    }
}
