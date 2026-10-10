namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A trigger started each time a signal is sent: <c>whenever button then run 1</c>, or a signal written in place,
    /// <c>whenever ext.'finishes-closing' on [ inner ] then run 2</c>.
    /// </summary>
    /// <remarks>
    /// A signal comes two ways, and which one is the block's business rather than the author's. A button, a timer, a
    /// sensor and an event controller send theirs through a toolbar slot, so Sprocket puts its own action in the slot and
    /// waits to be called, which is the game's own arrangement and not a second one beside it. A block's change, such as a
    /// door finishing closing, is one Sprocket watches for itself, as the change in the value it is.
    /// <para>
    /// Named, its one source is the variable or signal slot that holds the signal. Written in place, the sources are the
    /// blocks it is sent from, the member is <see cref="MomentTrigger.EventId" />, and a toolbar slot says where on their
    /// toolbars it is sent from. With no sources it says only that something on the construct has to start this.
    /// </para>
    /// </remarks>
    public sealed class SignalTrigger : MomentTrigger
    {
        /// <summary>A trigger started by a signal, named by no slot or variable yet.</summary>
        public SignalTrigger() : base("")
        {
        }

        /// <summary>A trigger started by the signal <paramref name="eventId" />.</summary>
        public SignalTrigger(string eventId) : base(eventId)
        {
        }

        /// <summary>The namespace the signal written in place is qualified by, or empty where the trigger names its signal.</summary>
        public string Provider { get; set; } = "";

        /// <summary>The slot on its blocks' toolbars a signal written in place is sent from, or null for a block's own change.</summary>
        public ToolbarSlot At { get; set; }

        /// <summary>Whether the signal is written in place, on the blocks it is sent from, rather than named.</summary>
        public bool IsWritten => Provider.Length > 0;

        /// <summary>Whether this is a block's own change, which Sprocket watches for itself, rather than a toolbar slot.</summary>
        public bool IsWatched => IsWritten && At == null;

        /// <summary>The signal part or variable this names, or empty where it names none or is written in place.</summary>
        public string Part => !IsWritten && Sources.Count > 0 ? Sources[0] : "";

        /// <summary>The trigger that is the signal part <paramref name="part" />, answered in each instance by its toolbar slots.</summary>
        public static SignalTrigger Named(string part)
        {
            var trigger = new SignalTrigger();
            trigger.Sources.Add(part);
            return trigger;
        }

        /// <summary>
        /// The trigger that is the signal <paramref name="eventId" /> qualified by <paramref name="provider" />, written in
        /// place and sent from any of the blocks added to its sources.
        /// </summary>
        public static SignalTrigger Written(string provider, string eventId)
        {
            var trigger = new SignalTrigger(eventId);
            trigger.Provider = provider;
            return trigger;
        }
    }
}
