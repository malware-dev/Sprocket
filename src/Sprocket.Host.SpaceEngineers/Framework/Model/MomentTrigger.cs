using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A trigger about a moment on some blocks: "when the inner door finishes closing".
    /// </summary>
    public abstract class MomentTrigger : Trigger
    {
        readonly List<string> _sources = new List<string>();

        /// <summary>A trigger started by the moment <paramref name="eventId" />, with no blocks named yet.</summary>
        protected MomentTrigger(string eventId)
        {
            EventId = eventId;
        }

        /// <summary>
        /// The blocks this is about, as declarations.
        /// </summary>
        public IList<string> Sources => _sources;

        /// <summary>The <see cref="Game.Members.EntityEvent" /> id this is about.</summary>
        public string EventId { get; set; }
    }
}
