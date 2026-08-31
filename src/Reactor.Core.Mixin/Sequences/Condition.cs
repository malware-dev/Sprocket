using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// A situation: these entities are in this state.
    /// </summary>
    /// <remarks>
    /// Several entities means <em>all</em> of them - "once the doors are shut" is only
    /// true when every named door is shut.
    ///
    /// Note this is the opposite of a trigger, where several entities means any one of
    /// them. That asymmetry is not an inconsistency; it is how the two sentences read
    /// out loud. "When any of these opens" is a moment that can be caused by one
    /// thing. "Once these are shut" is a situation that is not true until all of them
    /// are.
    /// </remarks>
    public sealed class Condition
    {
        readonly List<string> _entities = new List<string>();

        public Condition(string stateId)
        {
            StateId = stateId;
        }

        public IList<string> Entities => _entities;

        /// <summary>The <see cref="World.EntityState"/> id every entity must be in.</summary>
        public string StateId { get; set; }
    }
}
