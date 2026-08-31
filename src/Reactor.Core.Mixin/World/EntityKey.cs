namespace Reactor.World
{
    /// <summary>
    /// One way of saying which entity was meant.
    /// </summary>
    /// <remarks>
    /// <b>Core does not know what a key means, and must not learn.</b> A host mints these and a host
    /// reads them back; core parses values and lists of values and stores what it read. So what each
    /// one is - an id, a position on the grid, a name, something nobody has thought of - is the
    /// host's, and a new sort of key costs core nothing.
    /// <para>
    /// Untagged for that reason. A list like <c>id …, at …, name …</c> would put identity semantics
    /// into core's parser and freeze them there, where they become a compatibility promise.
    /// </para>
    /// </remarks>
    public struct EntityKey
    {
        public EntityKey(ValueKind kind, EntityValue value) : this()
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>What sort of value this is, so it can be read back and written down.</summary>
        public ValueKind Kind { get; private set; }

        public EntityValue Value { get; private set; }
    }
}
