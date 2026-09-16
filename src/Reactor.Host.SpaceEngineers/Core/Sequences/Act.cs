using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// A step that does something to some of the system's parts.
    /// </summary>
    /// <remarks>
    /// It starts the action and the sequence carries straight on. Holding until the doors have
    /// actually shut is <see cref="Await"/>, which is a line of its own - so acting on several
    /// things at once needs nothing said, and waiting is what has to be asked for.
    /// </remarks>
    public sealed class Act : Step
    {
        readonly List<string> _targets = new List<string>();

        public Act(string actionId)
        {
            ActionId = actionId;
        }

        /// <summary>The <see cref="World.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        /// <summary>The parts affected. All of them, together, in this step.</summary>
        public IList<string> Targets => _targets;

        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);
    }
}
