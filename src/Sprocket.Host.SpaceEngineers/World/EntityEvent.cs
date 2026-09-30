
namespace Sprocket.Host.SpaceEngineers.World
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

        /// <summary>One the construct answers, which has no reading behind it at all.</summary>
        public EntityEvent(string id, string text)
        {
            Id = id;
            PropertyId = "";
            Text = text;
            Answered = true;
        }

        /// <summary>Stable identifier, referenced by saved rules.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// Whether something on the construct answers this rather than the block reporting it.
        /// </summary>
        /// <remarks>
        /// A button is the case: it raises nothing until a slot on its toolbar holds something, and
        /// what it then runs is decided by that slot rather than by anything a rule could sample. A
        /// timer, a sensor and an event controller are the same, because each is a toolbar.
        /// <para>
        /// So there is nothing to watch, and watching anyway is worse than useless: the press arrives
        /// by the slot invoking Sprocket's action, and a rule sampling for it as well starts the same
        /// sequence twice on one press.
        /// </para>
        /// </remarks>
        public bool Answered { get; private set; }

        /// <summary>Whether the block says when this may have happened, so it is looked for then rather than only on a cadence.</summary>
        /// <remarks>See <see cref="IAnnounces" />. Set by the extension that knows the block raises it.</remarks>
        public bool Announced { get; private set; }

        /// <summary>Says the block announces this.</summary>
        internal void Announce()
        {
            Announced = true;
        }

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
