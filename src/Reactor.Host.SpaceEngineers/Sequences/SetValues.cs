using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Sequences
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
            _was = Copy(was);
            _now = Copy(now);
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

            act.Values.Clear();
            for (var i = 0; i < values.Count; i++)
                act.Values.Add(values[i]);
        }

        static List<Literal> Copy(IList<Literal> values)
        {
            var copy = new List<Literal>();
            if (values == null)
                return copy;

            for (var i = 0; i < values.Count; i++)
                copy.Add(values[i]);

            return copy;
        }

        /// <summary>
        /// Whether two lists say the same thing.
        /// </summary>
        /// <remarks>
        /// By how each one reads, which is what a value is for: two that write themselves the same
        /// way are the same value, and comparing the kinds as well would call a step changed for a
        /// difference nothing can see.
        /// </remarks>
        static bool Same(List<Literal> was, List<Literal> now)
        {
            if (was.Count != now.Count)
                return false;

            for (var i = 0; i < was.Count; i++)
            {
                if (was[i].Said != now[i].Said)
                    return false;
            }

            return true;
        }
    }
}
