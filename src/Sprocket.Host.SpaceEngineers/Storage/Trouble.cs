using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// What was wrong with a script, and what was only worth a warning, first thing first.
    /// </summary>
    /// <remarks>
    /// Some trouble is only visible once a statement is understood - two systems with the same identity, a
    /// switch that says neither on nor off - so the reader needs somewhere to say it as well as the parser.
    /// Only the first of each is kept, the same as the parser does, and the line is worked out only for
    /// that one. A warning never stops a program being saved; trouble does.
    /// </remarks>
    sealed class Trouble
    {
        readonly string _script;
        readonly List<Owed> _owed = new List<Owed>();
        readonly Dictionary<Step, Statement> _written = new Dictionary<Step, Statement>();
        readonly HashSet<string> _taken = new HashSet<string>(StringComparer.Ordinal);

        public Trouble(string script, string already)
        {
            _script = script;
            What = already ?? "";
        }

        /// <summary>What was wrong with the first thing that was wrong, or empty.</summary>
        public string What { get; private set; }

        /// <summary>The first thing that read but looks like a mistake, or empty.</summary>
        public string Warning { get; private set; } = "";

        public void Say(string what)
        {
            if (What.Length == 0)
                What = what;
        }

        public void Say(Statement at, string what)
        {
            if (What.Length == 0)
                What = Line(at) + what;
        }

        /// <summary>
        /// Says that slot <paramref name="at" /> on <paramref name="block" /> starts something, and is trouble where something
        /// read before already starts something from it.
        /// </summary>
        /// <remarks>A toolbar slot holds one action, so a second trigger on it could never be wired.</remarks>
        public void Takes(Statement statement, string block, ToolbarSlot at)
        {
            if (!_taken.Add(block + "\n" + at.EventId))
                Say(statement, Pen.Spelling("", block) + ":toolbar(" + at.Toolbar + ", " + at.Page + ", " + at.Slot
                    + ") already starts something; a toolbar slot holds one action.");
        }

        public void Warn(Statement at, string what)
        {
            if (Warning.Length == 0)
                Warning = Line(at) + what;
        }

        /// <summary>Remembers that <paramref name="step" /> was written at <paramref name="at" />, for trouble found about it once the whole script is read.</summary>
        public void Wrote(Statement at, Step step)
        {
            _written[step] = at;
        }

        /// <summary>Where <paramref name="step" /> was written, or <paramref name="otherwise" /> where it was not remembered.</summary>
        public Statement Where(Step step, Statement otherwise)
        {
            Statement at;
            return _written.TryGetValue(step, out at) ? at : otherwise;
        }

        /// <summary>
        /// Says <paramref name="at" /> names <paramref name="name" />, a slot of the system blueprint <paramref name="system" />:
        /// a trigger slot where <paramref name="trigger" /> says so, and a slot for a block otherwise.
        /// </summary>
        /// <remarks>Judged by <see cref="Settle" />, once the whole script is read, because a slot may be written below what names it.</remarks>
        public void Owes(Statement at, string name, AutomationSystem system, bool trigger)
        {
            var owed = new Owed();
            owed.At = at;
            owed.Name = name;
            owed.System = system;
            owed.Trigger = trigger;
            _owed.Add(owed);
        }

        /// <summary>Says what is wrong with every slot owed that its system blueprint has nothing of the right sort called.</summary>
        public void Settle()
        {
            for (var i = 0; i < _owed.Count; i++)
            {
                var owed = _owed[i];
                var part = owed.System.FindPart(owed.Name);
                if (part == null)
                    Say(owed.At, "no slot called '" + owed.Name + "'.");
                else if (part.IsTrigger && !owed.Trigger)
                    Say(owed.At, "'" + owed.Name + "' is a trigger slot; expected a slot for a block.");
                else if (!part.IsTrigger && owed.Trigger)
                    Say(owed.At, "'" + owed.Name + "' is a slot for a block; expected a trigger slot.");
            }
        }

        /// <summary>Where <paramref name="at" /> starts, as trouble begins: <c>Line 5: </c>.</summary>
        public string Line(Statement at)
        {
            return "Line " + new TextPtr(_script, at.At).FindLineNo() + ": ";
        }

        /// <summary>A name a statement uses, waiting to be judged.</summary>
        struct Owed
        {
            public Statement At;
            public string Name;
            public AutomationSystem System;
            public bool Trigger;
        }
    }
}
