using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A claim about which thing in the world was meant.
    /// </summary>
    /// <remarks>
    /// Not a value, and that is the whole difficulty. An id is a value; <i>the inner door</i> is a
    /// claim, and it has to survive renames, rebuilds, blueprints, and being handed to somebody who
    /// has never seen the ship it was written on.
    /// <para>
    /// Three parts. The <see cref="Name" /> is structural and means nothing - it is what the rest of
    /// the script points at, and may be regenerated on every save. The <see cref="KindId" /> is a
    /// constraint rather than a matcher, so a key that lands on something that is not a door is
    /// refused before it can bind, and it is the one part still useful when every key has failed:
    /// it says what belongs in the slot. The <see cref="Keys" /> are a plain ordered list, tried in
    /// order.
    /// </para>
    /// <para>
    /// An empty list is legal. A declaration can exist with nothing to bind it, which is the state a
    /// configurator needs and costs nothing before there is one.
    /// </para>
    /// </remarks>
    public sealed class EntityRef
    {
        readonly List<EntityKey> _keys = new List<EntityKey>();

        public EntityRef(string name, string kindId)
        {
            Name = name;
            KindId = kindId ?? "";
        }

        /// <summary>What the script calls this. Nothing reads meaning from it.</summary>
        public string Name { get; private set; }

        /// <summary>
        /// The kind anything binding here must be, or empty for no constraint.
        /// </summary>
        /// <remarks>
        /// A constraint and not a key. It never finds anything; it refuses things the keys found.
        /// </remarks>
        public string KindId { get; private set; }

        /// <summary>In priority order, strongest first. The host minted these and only it reads them.</summary>
        public List<EntityKey> Keys { get { return _keys; } }
    }
}
