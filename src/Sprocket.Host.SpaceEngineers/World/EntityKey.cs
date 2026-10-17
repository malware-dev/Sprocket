using VRageMath;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One way of saying which entity was meant.
    /// </summary>
    public struct EntityKey
    {
        /// <summary>A key holding <paramref name="value" />, a value of the kind <paramref name="kind" />.</summary>
        public EntityKey(ValueKind kind, EntityValue value) : this()
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>A key that is a list of cells, written as a list of vectors.</summary>
        public EntityKey(Vector3I[] cells) : this()
        {
            Cells = cells;
        }

        /// <summary>What sort of value this is, so it can be read back and written down.</summary>
        public ValueKind Kind { get; private set; }

        /// <summary>The value the key holds, where it is a value rather than a list of cells.</summary>
        public EntityValue Value { get; private set; }

        /// <summary>The cells, where this key is a list of them rather than a value; otherwise null.</summary>
        public Vector3I[] Cells { get; private set; }
    }
}
