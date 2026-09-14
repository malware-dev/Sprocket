using VRage.Game.ModAPI.Ingame.Utilities;

namespace Reactor.Storage
{
    /// <summary>
    /// What was wrong with a script, first thing first.
    /// </summary>
    /// <remarks>
    /// Some trouble is only visible once a statement is understood - two systems with the same name, a
    /// switch that says neither on nor off - so the reader needs somewhere to say it as well as the parser.
    /// Only the first is kept, the same as the parser does, and the line is worked out only for that one.
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

        public void Say(string what)
        {
            if (What.Length == 0)
                What = what;
        }

        public void Say(Statement at, string what)
        {
            if (What.Length == 0)
                What = "line " + new TextPtr(_script, at.At).FindLineNo() + ": " + what;
        }
    }
}
