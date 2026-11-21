using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
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
            Key = MemberKey.Bare(eventId);
        }

        /// <summary>
        /// The blocks this is about, as declarations.
        /// </summary>
        public IList<string> Sources => _sources;

        /// <summary>The signal or value this is about, as the program holds it: qualified by its namespace, or bare.</summary>
        public MemberKey Key { get; set; }

        /// <summary>The <see cref="Game.Members.EntityEvent" /> id this is about, within its namespace.</summary>
        /// <remarks>Setting it keeps the namespace; what this is about is still named by <see cref="Key" />.</remarks>
        public string EventId
        {
            get { return Key.Id; }
            set { Key = Key.WithId(value); }
        }
    }
}
