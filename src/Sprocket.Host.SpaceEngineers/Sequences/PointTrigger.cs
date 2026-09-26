using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a trigger does: which sequence it is about, and whether it stops that one rather than
    /// starting it.
    /// </summary>
    /// <remarks>
    /// The trigger itself is kept rather than replaced, because what starts it has not changed - and
    /// the toolbar slot the construct holds names that trigger. Rebuilding it would claim the slot
    /// again for something that is, to the player, the same button.
    /// <para>
    /// A sequence may come and go with the pointing. Answering "do something" or "run a new
    /// sequence" makes one, and pointing away from a sequence nobody named leaves nothing that could
    /// ever run it again - so this carries both, and undoing puts each back where it was.
    /// </para>
    /// </remarks>
    sealed class PointTrigger : SystemEdit
    {
        readonly Automation _program;
        readonly Trigger _trigger;
        readonly Sequence _made;

        readonly int _wasRuns;
        readonly bool _wasStops;
        readonly int _nowRuns;
        readonly bool _nowStops;

        Sequence _orphan;

        readonly List<string> _sources;
        readonly string _eventId;

        public PointTrigger(Automation program, Trigger trigger, int runs, bool stops, Sequence made)
        {
            _program = program;
            _trigger = trigger;
            _made = made;

            _wasRuns = trigger.Runs;
            _wasStops = trigger.Stops;
            _nowRuns = runs;
            _nowStops = stops;

            var moment = trigger as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment == null ? null : moment.EventId;
        }

        public override string Label => "changing what it does";

        public override bool Changes => _wasRuns != _nowRuns || _wasStops != _nowStops;

        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Runs == _nowRuns && _trigger.Stops == _nowStops
                && Holds(system, _trigger, _sources, _eventId);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Runs == _wasRuns && _trigger.Stops == _wasStops
                && Holds(system, _trigger, _sources, _eventId);
        }

        public override void Apply(AutomationSystem system)
        {
            // Already there the first time, and put back by this on the way forward again.
            if (_made != null)
                _program.PutBack(_made);

            _trigger.Runs = _nowRuns;
            _trigger.Stops = _nowStops;

            _orphan = Orphan(system, _wasRuns);
            if (_orphan != null)
                _program.RemoveSequence(_orphan);
        }

        public override void Undo(AutomationSystem system)
        {
            if (_orphan != null)
                _program.PutBack(_orphan);

            _trigger.Runs = _wasRuns;
            _trigger.Stops = _wasStops;

            if (_made != null)
                _program.RemoveSequence(_made);
        }

        /// <summary>
        /// The sequence of that number, where nothing is left that could start it and nobody named
        /// it.
        /// </summary>
        /// <remarks>
        /// The same rule deleting a trigger follows: a sequence nobody named exists only to be the
        /// one thing some trigger does, and one that is named is a thing in its own right whatever
        /// stops pointing at it.
        /// </remarks>
        static Sequence Orphan(AutomationSystem system, int number)
        {
            Sequence sequence = system.FindSequence(number);
            if (sequence == null || sequence.HasName || system.Called(number))
                return null;

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].Runs == number)
                    return null;
            }

            return sequence;
        }
    }
}
