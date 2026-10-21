using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Moves a branch of an if to another place in its chain, with its test and its steps; the first branch is always the if,
    /// so a branch moved to the top makes the if that was there an otherwise if.
    /// </summary>
    /// <remarks>The lines that open the branches stay where they stand and take the tests in their new order; an otherwise is never moved.</remarks>
    public sealed class MoveBranch : SequenceEdit
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

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.MoveBranch;

        /// <inheritdoc />
        public override bool Changes => _from != _to;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            Move(sequence, _from, _to);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            Move(sequence, _to, _from);
        }

        void Move(Sequence sequence, int from, int to)
        {
            var steps = sequence.Steps;
            var lines = sequence.Branches(_open);
            var end = sequence.EndOf(_open);
            var openers = new List<Branch>();
            var tests = new List<BranchTest>();
            var notes = new List<string>();
            var bodies = new List<List<Step>>();

            for (var b = 0; b < lines.Count; b++)
            {
                var opener = (Branch)steps[lines[b]];
                openers.Add(opener);
                tests.Add(opener.Test);
                notes.Add(opener.Annotation);

                var body = new List<Step>();
                var next = b + 1 < lines.Count ? lines[b + 1] : end;
                for (var i = lines[b] + 1; i < next; i++)
                    body.Add(steps[i]);

                bodies.Add(body);
            }

            Shift(tests, from, to);
            Shift(notes, from, to);
            Shift(bodies, from, to);

            for (var i = end - 1; i >= _open; i--)
                steps.RemoveAt(i);

            var line = _open;
            for (var b = 0; b < openers.Count; b++)
            {
                openers[b].Test = tests[b];
                openers[b].Annotation = notes[b];
                steps.Insert(line++, openers[b]);
                for (var i = 0; i < bodies[b].Count; i++)
                    steps.Insert(line++, bodies[b][i]);
            }
        }

        static void Shift<T>(List<T> list, int from, int to)
        {
            var moving = list[from];
            list.RemoveAt(from);
            list.Insert(to, moving);
        }
    }
}
