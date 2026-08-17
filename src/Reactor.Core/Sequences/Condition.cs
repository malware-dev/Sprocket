using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// A situation: these devices are in this state.
    /// </summary>
    /// <remarks>
    /// Several devices means <em>all</em> of them - "once the doors are shut" is only
    /// true when every named door is shut.
    ///
    /// Note this is the opposite of a trigger, where several devices means any one of
    /// them. That asymmetry is not an inconsistency; it is how the two sentences read
    /// out loud. "When any of these opens" is a moment that can be caused by one
    /// thing. "Once these are shut" is a situation that is not true until all of them
    /// are.
    /// </remarks>
    public sealed class Condition
    {
        readonly List<long> _devices = new List<long>();

        public Condition(string stateId)
        {
            StateId = stateId;
        }

        public IList<long> Devices { get { return _devices; } }

        /// <summary>The <see cref="World.DeviceState"/> id every device must be in.</summary>
        public string StateId { get; set; }
    }
}
