namespace Reactor.World
{
    /// <summary>
    /// Something that can be read off an entity: whether a door is open, how full a tank is.
    /// </summary>
    /// <remarks>
    /// The primitive everything else is built from. An <see cref="EntityEvent"/> is a change
    /// in one of these; an <see cref="EntityState"/> is a relation against one. Neither is a
    /// separate sort of thing the host has to keep in step by hand - they are two ways of
    /// talking about the same reading, which is why one signal can yield both "finishes
    /// opening" and "is open" without saying anything twice.
    ///
    /// Declared rather than implied. A set of readings that existed only in whatever ids a kind's
    /// events and states happened to mention could not be shown to anybody, and showing what a thing
    /// can tell you about itself is most of what a diagnosis screen is.
    /// </remarks>
    public sealed class EntityProperty
    {
        public EntityProperty(string id, ValueKind kind, string label)
        {
            Id = id;
            Kind = kind;
            Label = label;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        public ValueKind Kind { get; private set; }

        /// <summary>
        /// What the reading is called, e.g. "fill level".
        /// </summary>
        /// <remarks>
        /// A plain noun phrase, not a sentence fragment. States and events carry the wording
        /// that has to read aloud; this is a label on a dial.
        /// </remarks>
        public string Label { get; private set; }
    }
}
