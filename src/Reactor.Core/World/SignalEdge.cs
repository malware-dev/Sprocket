namespace Reactor.World
{
    /// <summary>
    /// Which transition of a signal counts as the event.
    /// </summary>
    /// <remarks>
    /// Reactor triggers are strictly edge-based: they describe a *moment*, never a
    /// situation. This is deliberate. Trigger-action systems reliably trip users up
    /// by letting "when the door opens" and "while the door is open" look like the
    /// same sentence when they behave completely differently, so Reactor only offers
    /// the first kind and will express the second as a separate, visibly different
    /// part of the sentence when conditions arrive.
    /// </remarks>
    public enum SignalEdge
    {
        /// <summary>The signal just became true.</summary>
        Rising,

        /// <summary>The signal just became false.</summary>
        Falling
    }
}
