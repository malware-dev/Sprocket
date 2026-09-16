namespace Reactor.World
{
    /// <summary>
    /// Which change in a reading counts as the event.
    /// </summary>
    /// <remarks>
    /// Reactor triggers are strictly edge-based: they describe a *moment*, never a
    /// situation. This is deliberate. Trigger-action systems reliably trip users up
    /// by letting "when the door opens" and "while the door is open" look like the
    /// same sentence when they behave completely differently, so Reactor only offers
    /// the first kind and expresses the second as a separate, visibly different part of the
    /// sentence.
    /// </remarks>
    public enum Edge
    {
        /// <summary>The reading just became true.</summary>
        Rising,

        /// <summary>The reading just became false.</summary>
        Falling
    }
}
