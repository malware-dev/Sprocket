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
        /// Says <paramref name="at" /> names <paramref name="name" />: one of <paramref name="system" />'s parts or a block, or
        /// a block where <paramref name="system" /> is null.
        /// </summary>
        /// <remarks>Judged by <see cref="Settle" />, once the whole script is read, because a block may be written below what names it.</remarks>
        public void Owes(Statement at, string name, AutomationSystem system)
        {
            var owed = new Owed();
            owed.At = at;
            owed.Name = name;
            owed.System = system;
            _owed.Add(owed);
        }

        /// <summary>Says what is wrong with every name owed that <paramref name="program" /> has nothing called.</summary>
        public void Settle(Automation program)
        {
            for (var i = 0; i < _owed.Count; i++)
            {
                var owed = _owed[i];
                if (program.FindRef(owed.Name) != null)
                    continue;

                var part = owed.System == null ? null : owed.System.FindPart(owed.Name);
                if (owed.System == null)
                    Say(owed.At, "no block called '" + owed.Name + "'.");
                else if (part == null)
                    Say(owed.At, "no part or block called '" + owed.Name + "'.");
                else if (part.IsTrigger)
                    Say(owed.At, "'" + owed.Name + "' is a trigger part; expected a block part.");
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
        }
    }
}
