using System.Collections.Generic;

namespace Reactor.Sequences
{
    sealed class SetCadence : SequenceEdit
    {
        readonly int _was;
        readonly int _now;

        public SetCadence(int was, int now)
        {
            _was = was;
            _now = now;
        }

        public override string Label => "changing how often it runs";

        public override bool Changes => _was != _now;

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
