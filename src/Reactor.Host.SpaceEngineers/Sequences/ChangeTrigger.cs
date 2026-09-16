namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What starts a sequence: the blocks, the moment, and how it reaches Reactor. One trip to the
    /// picker, so one edit.
    /// </summary>
    /// <remarks>
    /// The whole trigger rather than its fields, because one answer can change which sort it is: a
    /// door means Reactor watches for the moment, a button means the construct announces it. Both
    /// objects are kept, so undo puts back the one that was there rather than rebuilding something
    /// that reads the same.
    /// </remarks>
    sealed class ChangeTrigger : SequenceEdit
    {
        readonly MomentTrigger _was;
        readonly MomentTrigger _now;

        public ChangeTrigger(MomentTrigger was, MomentTrigger now)
        {
            _was = was;
            _now = now;
            _now.Runs = was.Runs;
            _now.Annotation = was.Annotation;
        }

        public override string Label => "changing when it runs";

        public override bool Changes
        {
            get
            {
                if (_was.GetType() != _now.GetType())
                    return true;

                if (_was.EventId != _now.EventId || !Same(_was.Sources, _now.Sources))
                    return true;

                var wasWhen = _was as WhenTrigger;
                var nowWhen = _now as WhenTrigger;
                return wasWhen != null && nowWhen != null && wasWhen.RequiresAll != nowWhen.RequiresAll;
            }
        }

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _was, _now);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _now, _was);
        }

        static void Put(Sequence sequence, Trigger was, Trigger now)
        {
            if (sequence.System != null)
                sequence.System.ReplaceTrigger(was, now);
        }
    }
}
