namespace Reactor.World
{
    /// <summary>
    /// Something that can happen to an entity, and which a rule can listen for.
    /// </summary>
    /// <remarks>
    /// An edge on one of the entity's properties: the moment its reading changes. The same
    /// property backs the conditions somebody writes about it - "only if the door is open" -
    /// so a host describes a reading once and gets both the moment and the situation from it.
    ///
    /// Edges are for readings that turn on and off, which today means flags. What the
    /// equivalent is for a number - crossing a threshold, moving at all - is not decided, and
    /// a host declaring an event on a numeric property is declaring something core cannot
    /// currently watch.
    /// </remarks>
    public sealed class EntityEvent
    {
        public EntityEvent(string id, string propertyId, Edge edge, string text)
        {
            Id = id;
            PropertyId = propertyId;
            Edge = edge;
            Text = text;
        }

        /// <summary>Stable identifier, referenced by saved rules.</summary>
        public string Id { get; private set; }

        /// <summary>The property on the entity this event watches.</summary>
        public string PropertyId { get; private set; }

        /// <summary>Which transition of <see cref="PropertyId"/> fires this event.</summary>
        public Edge Edge { get; private set; }

        /// <summary>
        /// How this reads inside a sentence, e.g. "finishes opening". Phrased to
        /// complete "When the hangar door ___", never as a technical name.
        /// </summary>
        public string Text { get; private set; }
    }
}
