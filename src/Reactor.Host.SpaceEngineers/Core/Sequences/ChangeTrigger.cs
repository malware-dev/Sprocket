using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// What starts a sequence: the things watched, what has to happen to them, and whether it
    /// takes all of them at once. One trip to the picker, so one edit.
    /// </summary>
    sealed class ChangeTrigger : SequenceEdit
    {
        readonly List<string> _wasSources;
        readonly string _wasEvent;
        readonly bool _wasAll;
        readonly List<string> _nowSources;
        readonly string _nowEvent;
        readonly bool _nowAll;

        public ChangeTrigger(WhenTrigger trigger, IList<string> sources, string eventId, bool all)
        {
            _wasSources = CopyOf(trigger.Sources);
            _wasEvent = trigger.EventId;
            _wasAll = trigger.RequiresAll;
            _nowSources = CopyOf(sources);
            _nowEvent = eventId;
            _nowAll = all;
        }

        public override string Label => "changing when it runs";

        public override bool Changes =>
            _wasEvent != _nowEvent || _wasAll != _nowAll
                                   || !Same(_wasSources, _nowSources);

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _nowSources, _nowEvent, _nowAll);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _wasSources, _wasEvent, _wasAll);
        }

        static void Put(Sequence sequence, IList<string> sources, string eventId, bool all)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            Refill(when.Sources, sources);
            when.EventId = eventId;
            when.RequiresAll = all;
        }
    }
}
