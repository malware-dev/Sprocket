using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// Something the machine does: the ordered steps a trigger, or a step of another sequence, runs.
    /// </summary>
    public sealed class Sequence : ICategorized, IExclusive
    {
        readonly List<Step> _steps = new List<Step>();

        /// <summary>An empty sequence with the identity <paramref name="id" />.</summary>
        public Sequence(int id)
        {
            Id = id;
            Annotation = "";
        }

        /// <summary>
        /// A handle for this session only.
        /// </summary>
        /// <remarks>Unique across the program, never saved, and meaningless after a reload.</remarks>
        public int Id { get; private set; }

        /// <summary>
        /// Which sequence of its system this is: 1, 2, 3.
        /// </summary>
        /// <remarks>Saved, and named by buttons, so it never changes once chosen.</remarks>
        public int Number { get; set; }

        /// <summary>Whether this is what the runtime makes to do a trigger's own act, which has no number and is no sequence of the program.</summary>
        public bool IsAlone => Number <= 0;

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>What the player calls this, such as "Let me out", or empty.</summary>
        public string Name { get; set; }

        /// <summary>The category the sequence is listed under on the Sequences tab, with the sequences beside it under the same one, or empty for none.</summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// The id of the sequences of its system it runs one at a time with, in each instance, or empty where it runs whenever it is
        /// started.
        /// </summary>
        public string Exclusive { get; set; } = "";

        /// <summary>Whether somebody has given it a name.</summary>
        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// The first thing that starts this sequence, or null where nothing does.
        /// </summary>
        public Trigger Trigger => System?.TriggerFor(Number);

        /// <summary>Its steps, in the order they run.</summary>
        public IList<Step> Steps => _steps;

        /// <summary>The system this belongs to.</summary>
        public AutomationSystem System { get; set; }

        /// <summary>
        /// Whether it may run on <paramref name="instance" />: its system switched on, and that one switched on.
        /// </summary>
        public bool CanRun(SystemInstance instance)
        {
            return (System == null || System.Enabled) && (instance == null || instance.Enabled);
        }

        /// <summary>The line of its <see cref="Catch" />, or -1 where it has none.</summary>
        /// <remarks>Walks the lines, so it is asked as a run fails or ends, never every tick.</remarks>
        public int CatchAt => Find<Catch>();

        /// <summary>The line of its <see cref="Finally" />, or -1 where it has none.</summary>
        /// <remarks>Walks the lines, so it is asked as a run fails or ends, never every tick.</remarks>
        public int FinallyAt => Find<Finally>();

        /// <summary>Where its own steps end: the line of its first <see cref="Handler" />, or the number of lines where it has none.</summary>
        /// <remarks>Walks the lines, so it is asked as a run fails or ends, never every tick.</remarks>
        public int HandlersAt
        {
            get
            {
                for (var i = 0; i < _steps.Count; i++)
                {
                    if (_steps[i] is Handler)
                        return i;
                }

                return _steps.Count;
            }
        }

        int Find<T>() where T : Handler
        {
            for (var i = 0; i < _steps.Count; i++)
            {
                if (_steps[i] is T)
                    return i;
            }

            return -1;
        }

        /// <summary>The line that ends the branch opened at <paramref name="at" />: the next <see cref="Else" /> of its <see cref="If" />, or its <see cref="EndIf" />.</summary>
        /// <remarks>Walks the lines of the branch, so it is called when a run reaches a branch, never every tick.</remarks>
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

        /// <summary>The number the line at <paramref name="at" /> is shown with: lines are counted from one, and the line closing an if is not counted.</summary>
        public int NumberOf(int at)
        {
            var number = 0;
            for (var i = 0; i <= at && i < _steps.Count; i++)
            {
                if (!(_steps[i] is EndIf))
                    number++;
            }

            return number;
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
