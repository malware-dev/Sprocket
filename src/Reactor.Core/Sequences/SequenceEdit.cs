using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// One change to a sequence, and how to take it back.
    /// </summary>
    /// <remarks>
    /// Edits mutate the sequence that is already there rather than rebuilding it. That is not
    /// a detail: Reactor is live, sequences run while they are being edited, and the runner
    /// and the open pages hold references to the very objects being changed. Anything that
    /// reconstructed the sequence in order to undo would leave all of them pointing at
    /// orphans.
    ///
    /// Each edit carries what it needs to reverse itself - the step it removed, the name it
    /// replaced - so undoing never has to go looking. That is the same shape a change needs to
    /// be sent to a server, which is where this has to end up anyway.
    ///
    /// The obvious risk is an <see cref="Undo"/> that does not exactly reverse its
    /// <see cref="Apply"/>, and it is a real one, because undo only ever runs when somebody is
    /// already recovering from a mistake. It is answered by a round-trip test per edit rather
    /// than by being careful: apply it, undo it, and the sequence must equal what it was.
    /// </remarks>
    abstract class SequenceEdit
    {
        /// <summary>What this did, in the words it will be offered back in.</summary>
        public abstract string Label { get; }

        public abstract void Apply(Sequence sequence);

        public abstract void Undo(Sequence sequence);

        /// <summary>
        /// Whether this actually changes anything.
        /// </summary>
        /// <remarks>
        /// Going to a picker and coming back having settled on what was already there is not
        /// an edit, and recording it would offer to undo something nobody did.
        /// </remarks>
        public virtual bool Changes { get { return true; } }

        /// <summary>
        /// Whether this changes the shape of the screen, rather than only what it says.
        /// </summary>
        /// <remarks>
        /// Renaming does not. Rebuilding for it would recreate the field being typed into and
        /// throw away the caret mid-word, and the heading is read every tick anyway.
        /// </remarks>
        public virtual bool ChangesShape { get { return true; } }

        protected static List<long> CopyOf(IList<long> ids)
        {
            var copy = new List<long>();
            for (var i = 0; i < ids.Count; i++)
                copy.Add(ids[i]);
            return copy;
        }

        protected static void Refill(IList<long> target, IList<long> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        protected static bool Same(IList<long> left, IList<long> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }

    sealed class AddStep : SequenceEdit
    {
        readonly string _label;
        readonly int _index;
        readonly Step _step;

        public AddStep(string label, int index, Step step)
        {
            _label = label;
            _index = index;
            _step = step;
        }

        public override string Label { get { return _label; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Steps.Insert(_index, _step);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps.RemoveAt(_index);
        }
    }

    sealed class RemoveStep : SequenceEdit
    {
        readonly int _index;
        readonly Step _step;

        public RemoveStep(int index, Step step)
        {
            _index = index;
            _step = step;
        }

        public override string Label { get { return "deleting a step"; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Steps.RemoveAt(_index);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps.Insert(_index, _step);
        }
    }

    sealed class MoveStep : SequenceEdit
    {
        readonly int _from;
        readonly int _to;

        public MoveStep(int from, int to)
        {
            _from = from;
            _to = to;
        }

        public override string Label { get { return "moving a step"; } }

        public override bool Changes { get { return _from != _to; } }

        public override void Apply(Sequence sequence)
        {
            Move(sequence, _from, _to);
        }

        public override void Undo(Sequence sequence)
        {
            Move(sequence, _to, _from);
        }

        static void Move(Sequence sequence, int from, int to)
        {
            var step = sequence.Steps[from];
            sequence.Steps.RemoveAt(from);
            sequence.Steps.Insert(to, step);
        }
    }

    /// <summary>
    /// What a step points at and what it does, which are chosen on one trip and land together.
    /// </summary>
    sealed class ChangeStep : SequenceEdit
    {
        readonly int _index;
        readonly List<long> _wasTargets;
        readonly string _wasAction;
        readonly List<long> _nowTargets;
        readonly string _nowAction;

        public ChangeStep(int index, Step step, IList<long> targets, string actionId)
        {
            _index = index;
            _wasTargets = CopyOf(step.Targets);
            _wasAction = step.ActionId;
            _nowTargets = CopyOf(targets);
            _nowAction = actionId;
        }

        public override string Label { get { return "changing a step"; } }

        public override bool Changes
        {
            get { return _wasAction != _nowAction || !Same(_wasTargets, _nowTargets); }
        }

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowTargets, _nowAction);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasTargets, _wasAction);
        }

        void Put(Sequence sequence, IList<long> targets, string action)
        {
            var step = sequence.Steps[_index];
            Refill(step.Targets, targets);
            step.ActionId = action;
        }
    }

    sealed class SetWait : SequenceEdit
    {
        readonly int _index;
        readonly int _was;
        readonly int _now;

        public SetWait(int index, int was, int now)
        {
            _index = index;
            _was = was;
            _now = now;
        }

        public override string Label { get { return "changing a wait"; } }

        public override bool Changes { get { return _was != _now; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Steps[_index].Gate = new AfterGate(_now);
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Steps[_index].Gate = new AfterGate(_was);
        }
    }

    sealed class Rename : SequenceEdit
    {
        readonly string _was;
        readonly string _now;

        public Rename(string was, string now)
        {
            _was = was;
            _now = now;
        }

        public override string Label { get { return "renaming"; } }

        public override bool ChangesShape { get { return false; } }

        public override bool Changes { get { return _was != _now; } }

        public override void Apply(Sequence sequence)
        {
            sequence.Name = _now;
        }

        public override void Undo(Sequence sequence)
        {
            sequence.Name = _was;
        }
    }

    /// <summary>
    /// What starts a sequence: the things watched, what has to happen to them, and whether it
    /// takes all of them at once. One trip to the picker, so one edit.
    /// </summary>
    sealed class ChangeTrigger : SequenceEdit
    {
        readonly List<long> _wasSources;
        readonly string _wasEvent;
        readonly bool _wasAll;
        readonly List<long> _nowSources;
        readonly string _nowEvent;
        readonly bool _nowAll;

        public ChangeTrigger(WhenTrigger trigger, IList<long> sources, string eventId, bool all)
        {
            _wasSources = CopyOf(trigger.Sources);
            _wasEvent = trigger.EventId;
            _wasAll = trigger.RequiresAll;
            _nowSources = CopyOf(sources);
            _nowEvent = eventId;
            _nowAll = all;
        }

        public override string Label { get { return "changing when it runs"; } }

        public override bool Changes
        {
            get
            {
                return _wasEvent != _nowEvent || _wasAll != _nowAll
                    || !Same(_wasSources, _nowSources);
            }
        }

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowSources, _nowEvent, _nowAll);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasSources, _wasEvent, _wasAll);
        }

        static void Put(Sequence sequence, IList<long> sources, string eventId, bool all)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            Refill(when.Sources, sources);
            when.EventId = eventId;
            when.RequiresAll = all;
        }
    }

    sealed class SetCadence : SequenceEdit
    {
        readonly int _was;
        readonly int _now;

        public SetCadence(int was, int now)
        {
            _was = was;
            _now = now;
        }

        public override string Label { get { return "changing how often it runs"; } }

        public override bool Changes { get { return _was != _now; } }

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);
        }

        static void Put(Sequence sequence, int ticks)
        {
            var every = sequence.Trigger as EveryTrigger;
            if (every != null)
                every.IntervalTicks = ticks;
        }
    }
}
