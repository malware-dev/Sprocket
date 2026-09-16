using VRage.Game.ModAPI.Ingame.Utilities;

namespace Reactor.Host.SpaceEngineers.Storage
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

        string Line(Statement at)
        {
            return "line " + new TextPtr(_script, at.At).FindLineNo() + ": ";
        }
    }
}
