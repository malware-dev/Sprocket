namespace Reactor.World
{
    /// <summary>
    /// Something that can happen to a device, and which a rule can listen for.
    /// </summary>
    /// <remarks>
    /// Every event is defined as an edge on a named boolean signal the device
    /// exposes. Keeping the whole vocabulary reducible to booleans means the host
    /// only ever has to answer one question per signal, and it means the same
    /// signals can later back conditions ("only if the door is open") without the
    /// host having to describe anything twice.
    /// </remarks>
    public sealed class DeviceEvent
    {
        public DeviceEvent(string id, string signalId, SignalEdge edge, string text)
        {
            Id = id;
            SignalId = signalId;
            Edge = edge;
            Text = text;
        }

        /// <summary>Stable identifier, referenced by saved rules.</summary>
        public string Id { get; private set; }

        /// <summary>The boolean signal on the device this event watches.</summary>
        public string SignalId { get; private set; }

        /// <summary>Which transition of <see cref="SignalId"/> fires this event.</summary>
        public SignalEdge Edge { get; private set; }

        /// <summary>
        /// How this reads inside a sentence, e.g. "finishes opening". Phrased to
        /// complete "When the hangar door ___", never as a technical name.
        /// </summary>
        public string Text { get; private set; }
    }
}
