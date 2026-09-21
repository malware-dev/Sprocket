using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Changes what an action is given, all of it at once.
    /// </summary>
    /// <remarks>
    /// One edit for the whole list, because that is how it was asked for: the action says how many
    /// values it wants and nothing about what each one is, so there is no such thing as changing the
    /// second one on its own.
    /// </remarks>
    sealed class SetValues : SequenceEdit
    {
        readonly int _index;
        readonly List<Literal> _was;
        readonly List<Literal> _now;

        public SetValues(int index, IList<Literal> was, IList<Literal> now)
        {
            _index = index;
            _was = CopyOf(was);
            _now = CopyOf(now);
        }

        public override string Label => "changing what it is given";

        public override bool Changes => !Same(_was, _now);

        public override void Apply(Sequence sequence)
        {
            Put(sequence, _now);
        }

        public override void Undo(Sequence sequence)
        {
            Put(sequence, _was);
        }

        void Put(Sequence sequence, List<Literal> values)
        {
            if (_index < 0 || _index >= sequence.Steps.Count)
                return;

            var act = sequence.Steps[_index] as Act;
            if (act == null)
                return;

            Refill(act.Values, values);
        }
    }
}
