namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// A situation an entity can be in - "is open", "is moving", "is empty".
    /// </summary>
    /// <remarks>
    /// States and <see cref="EntityEvent"/>s are read off the same signals, but they
    /// are never interchangeable and the language keeps them apart on purpose.
    ///
    /// Both are ways of talking about one property. An event is a change in it - a moment,
    /// which happens and is then over. A state is a relation against it - a situation, true
    /// for as long as it is true. People reliably conflate the
    /// two - "when the door opens" and "while the door is open" feel like the same
    /// sentence - and the resulting bug is invisible, because the rule still reads
    /// correctly to whoever wrote it.
    ///
    /// So Reactor never offers a choice between them. Events only ever start a
    /// sequence; states only ever gate a step or guard an interlock. The distinction
    /// is structural rather than something the user has to hold in their head.
    /// </remarks>
    public sealed class EntityState
    {
        public EntityState(
            string id, string propertyId, ValueKind kind, Relation relation, EntityValue operand, string text)
        {
            Id = id;
            PropertyId = propertyId;
            Kind = kind;
            Relation = relation;
            Operand = operand;
            Text = text;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>The property this reads.</summary>
        public string PropertyId { get; private set; }

        /// <summary>
        /// What sort of reading that property gives.
        /// </summary>
        /// <remarks>
        /// Carried here because a value does not carry its own kind - the declaration does, and this is
        /// the declaration. Taken from the property when the state is added, so the two cannot disagree.
        /// </remarks>
        public ValueKind Kind { get; private set; }

        public Relation Relation { get; private set; }

        /// <summary>What the reading is measured against.</summary>
        public EntityValue Operand { get; private set; }

        /// <summary>
        /// How it reads in a sentence, e.g. "is shut". Phrased to complete "once the
        /// inner door ___", never as a technical name.
        /// </summary>
        public string Text { get; private set; }

        public bool Holds(Values values, EntityValue reading)
        {
            return Relations.Holds(values, Kind, reading, Relation, Operand);
        }
    }
}
