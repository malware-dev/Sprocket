using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Moves a branch of an if to another place in its chain, with its test and its steps; the first branch is always the if,
    /// so a branch moved to the top makes the if that was there an otherwise if.
    /// </summary>
    /// <remarks>
    /// The lines that open the branches stay where they stand and take the tests in their new order, so the if keeps what
    /// belongs to the whole of it, its switch among them. An otherwise comes last and is not moved.
    /// </remarks>
    sealed class MoveBranch : SequenceEdit
    {
        readonly int _open;
        readonly int _from;
        readonly int _to;

        /// <summary>Moves branch <paramref name="from" /> of the if at <paramref name="open" /> to be branch <paramref name="to" />, counted from the if's own.</summary>
        public MoveBranch(int open, int from, int to)
        {
            _open = open;
            _from = from;
            _to = to;
        }

        public override Change Change => Change.MoveBranch;

        public override bool Changes => _from != _to;

        public override void Apply(Sequence sequence)
        {
            Move(sequence, _from, _to);
        }

        public override void Undo(Sequence sequence)
        {
            Move(sequence, _to, _from);
        }

        void Move(Sequence sequence, int from, int to)
        {
            var steps = sequence.Steps;
            var openers = new List<Step>();
            var tests = new List<Comparison>();
            var notes = new List<string>();
            var bodies = new List<List<Step>>();

            var at = _open;
            while (at < steps.Count && !(steps[at] is EndIf))
            {
                var next = sequence.BranchAfter(at);
                openers.Add(steps[at]);
                tests.Add(Test(steps[at]));
                notes.Add(steps[at].Annotation);

                var body = new List<Step>();
                for (var i = at + 1; i < next; i++)
                    body.Add(steps[i]);

                bodies.Add(body);
                at = next;
            }

            Shift(tests, from, to);
            Shift(notes, from, to);
            Shift(bodies, from, to);

            for (var i = at - 1; i >= _open; i--)
                steps.RemoveAt(i);

            var line = _open;
            for (var b = 0; b < openers.Count; b++)
            {
                Put(openers[b], tests[b]);
                openers[b].Annotation = notes[b];
                steps.Insert(line++, openers[b]);
                for (var i = 0; i < bodies[b].Count; i++)
                    steps.Insert(line++, bodies[b][i]);
            }
        }

        static Comparison Test(Step opener)
        {
            var opens = opener as If;
            if (opens != null)
                return opens.Test;

            var branch = opener as Else;
            return branch == null ? null : branch.Test;
        }

        static void Put(Step opener, Comparison test)
        {
            var opens = opener as If;
            if (opens != null)
                opens.Test = test;

            var branch = opener as Else;
            if (branch != null)
                branch.Test = test;
        }

        static void Shift<T>(List<T> list, int from, int to)
        {
            var moving = list[from];
            list.RemoveAt(from);
            list.Insert(to, moving);
        }
    }
}
