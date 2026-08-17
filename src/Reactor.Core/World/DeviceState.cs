namespace Reactor.World
{
    /// <summary>
    /// A situation a device can be in - "is open", "is moving", "is empty".
    /// </summary>
    /// <remarks>
    /// States and <see cref="DeviceEvent"/>s are read off the same signals, but they
    /// are never interchangeable and the language keeps them apart on purpose.
    ///
    /// An event is a moment: it happens, and then it is over. A state is a
    /// situation: it is true for as long as it is true. People reliably conflate the
    /// two - "when the door opens" and "while the door is open" feel like the same
    /// sentence - and the resulting bug is invisible, because the rule still reads
    /// correctly to whoever wrote it.
    ///
    /// So Reactor never offers a choice between them. Events only ever start a
    /// sequence; states only ever gate a step or guard an interlock. The distinction
    /// is structural rather than something the user has to hold in their head.
    /// </remarks>
    public sealed class DeviceState
    {
        public DeviceState(string id, string signalId, bool whenSignal, string text)
        {
            Id = id;
            SignalId = signalId;
            WhenSignal = whenSignal;
            Text = text;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>The boolean signal this reads.</summary>
        public string SignalId { get; private set; }

        /// <summary>
        /// Whether the state holds when the signal is true. False lets a single
        /// signal describe both sides - "is open" and "is shut" off one reading.
        /// </summary>
        public bool WhenSignal { get; private set; }

        /// <summary>
        /// How it reads in a sentence, e.g. "is shut". Phrased to complete "once the
        /// inner door ___", never as a technical name.
        /// </summary>
        public string Text { get; private set; }
    }
}
