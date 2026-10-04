using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something the machine does: the ordered steps a trigger, or a step of another sequence, runs.
    /// </summary>
    /// <remarks>
    /// A sequence can be understood on its own, without reading any other sequence.
    /// That is what lets someone grow a program a piece at a time instead of holding
    /// the whole thing in their head, and it is the main thing separating this from a
    /// script.
    ///
    /// A sequence is run by its number, so the name is optional and decides nothing:
    /// it makes a list of twenty sequences legible, and a script may use it in place
    /// of the number. Naming is never required to get anything done.
    /// </remarks>
    public sealed class Sequence
    {
        readonly List<Step> _steps = new List<Step>();

        public Sequence(int id)
        {
            Id = id;
            Annotation = "";
        }

        /// <summary>
        /// A handle for this session only.
        /// </summary>
        /// <remarks>
        /// <b>Never saved.</b> It is unique across the whole program, which is what the runner, the editor's
        /// history and its commands need to tell sequences apart while it runs, and it means nothing after a
        /// reload. What a sequence is, to a save or a button, is its <see cref="Number" /> within its system.
        /// </remarks>
        public int Id { get; private set; }

        /// <summary>
        /// Which sequence of its system this is: 1, 2, 3.
        /// </summary>
        /// <remarks>
        /// Counted within the system rather than across the program, so a system carries its numbers with it
        /// when it is exported and imported, and a button naming a system and a number still means the same
        /// sequence. Chosen from the numbers the system already has - see
        /// <see cref="AutomationSystem.NextSequenceNumber" /> - and never changed afterwards, so a rename
        /// cannot unwire anything.
        /// </remarks>
        public int Number { get; set; }

        /// <summary>Whether this is what the runtime makes to do a trigger's own act, which has no number and is no sequence of the program.</summary>
        public bool IsAlone => Number <= 0;

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }

        /// <summary>What the user calls this, e.g. "Let me out". May be empty.</summary>
        public string Name { get; set; }

        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// The first thing that starts this sequence, or null where nothing does.
        /// </summary>
        /// <remarks>
        /// Asked of the system, which is where triggers live. A sequence with nothing to start it is
        /// ordinary rather than broken: somebody is halfway through building it, or it is only ever
        /// reached from another sequence.
        /// </remarks>
        public Trigger Trigger => System == null ? null : System.TriggerFor(Number);

        public IList<Step> Steps => _steps;

        /// <summary>The system this belongs to.</summary>
        public AutomationSystem System { get; set; }

        /// <summary>
        /// Whether it may run on <paramref name="instance" />: its system switched on, and that one switched on.
        /// </summary>
        /// <remarks>
        /// The system's switch and the one's are asked separately everywhere else, because "I am working on the airlock" and
        /// "I am rewiring the port airlock" are different intentions and each has to survive the other.
        /// </remarks>
        public bool CanRun(SystemInstance instance)
        {
            return (System == null || System.Enabled) && (instance == null || instance.Enabled);
        }

        /// <summary>The line that ends the branch opened at <paramref name="at" />: the next <see cref="Else" /> of its <see cref="If" />, or its <see cref="EndIf" />.</summary>
        /// <remarks>Walks the lines of the branch, so it is asked when a run reaches a branch rather than on every tick.</remarks>
        public int BranchAfter(int at)
        {
            var depth = 0;
            for (var i = at + 1; i < _steps.Count; i++)
            {
                if (_steps[i] is If)
                {
                    depth++;
                }
                else if (_steps[i] is EndIf)
                {
                    if (depth == 0)
                        return i;

                    depth--;
                }
                else if (_steps[i] is Else && depth == 0)
                {
                    return i;
                }
            }

            return _steps.Count;
        }

        /// <summary>The <see cref="EndIf" /> of the <see cref="If" /> whose branch opens at <paramref name="at" />.</summary>
        public int EndOf(int at)
        {
            while (at < _steps.Count && !(_steps[at] is EndIf))
                at = BranchAfter(at);

            return at;
        }

        /// <summary>The lines opening each branch of the <see cref="If" /> at <paramref name="open" />, in order, its own first.</summary>
        /// <remarks>Walks the if's lines, so it is asked when the if is changed or moved, never on a tick.</remarks>
        public List<int> Branches(int open)
        {
            var lines = new List<int>();
            for (var at = open; at < _steps.Count && !(_steps[at] is EndIf); at = BranchAfter(at))
                lines.Add(at);

            return lines;
        }

        /// <summary>The line of the <see cref="Else" /> of the <see cref="If" /> at <paramref name="open" /> that tests nothing, which comes last, or -1 where it has none.</summary>
        public int Otherwise(int open)
        {
            var end = EndOf(open);
            var last = -1;
            for (var at = open; at < end; at = BranchAfter(at))
                last = at;

            var branch = last > open ? _steps[last] as Branch : null;
            return branch != null && branch.Test == null ? last : -1;
        }

        /// <summary>The line a run goes on to from the step at <paramref name="at" />: past what is switched off, and out of a branch past the end of its if.</summary>
        public int After(int at)
        {
            var next = at + 1;
            while (next < _steps.Count)
            {
                var step = _steps[next];
                if ((step.IsOff && step is If) || step is Else)
                    next = EndOf(next) + 1;
                else if (step.IsOff || step is EndIf)
                    next++;
                else
                    break;
            }

            return next;
        }

        /// <summary>The <see cref="If" /> that the <see cref="Else" /> or <see cref="EndIf" /> at <paramref name="at" /> belongs to, or -1.</summary>
        public int OpenOf(int at)
        {
            var depth = 0;
            for (var i = at - 1; i >= 0; i--)
            {
                if (_steps[i] is EndIf)
                {
                    depth++;
                }
                else if (_steps[i] is If)
                {
                    if (depth == 0)
                        return i;

                    depth--;
                }
            }

            return -1;
        }
    }
}
