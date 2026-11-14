using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A claim about which thing in the world was meant.
    /// </summary>
    /// <remarks>The keys are tried in order, and an empty list of them is legal.</remarks>
    public sealed class EntityRef
    {
        readonly List<EntityKey> _keys = new List<EntityKey>();

        /// <summary>A variable called <paramref name="name" />, holding a block of the kind <paramref name="kindId" />.</summary>
        public EntityRef(string name, string kindId)
        {
            Name = name;
            KindId = kindId ?? "";
        }

        /// <summary>What the script calls this.</summary>
        public string Name { get; private set; }

        /// <summary>
        /// The kind anything binding here must be, or empty for no constraint.
        /// </summary>
        public string KindId { get; private set; }

        /// <summary>What somebody wrote above it, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>The host's keys for the block, strongest first.</summary>
        public List<EntityKey> Keys => _keys;
    }
}
