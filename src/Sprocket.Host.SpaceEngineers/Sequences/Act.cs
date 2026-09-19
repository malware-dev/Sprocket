using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
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

        /// <summary>
        /// The values the action has to be given, in the order it declares them.
        /// </summary>
        /// <remarks>
        /// Each carries its own kind, because nothing else does: an action declares how many values
        /// it wants and not one thing about what they are, so the program is the only record of
        /// whether somebody wrote a number, a flag or a colour.
        /// <para>
        /// Empty for every action of Sprocket's own, and for the great majority of the game's.
        /// </para>
        /// </remarks>
        public IList<Literal> Values => _values;

        readonly List<Literal> _values = new List<Literal>();

        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);
    }
}
