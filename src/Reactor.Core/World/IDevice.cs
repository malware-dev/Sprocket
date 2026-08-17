namespace Reactor.World
{
    /// <summary>
    /// One addressable thing on the machine - a door, a light, a sensor.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Stable identity. Space Engineers gives blocks a unique id that survives
        /// renaming and save/load, so rules reference this and never the name. The
        /// name is a human helper and is free to change under us.
        /// </summary>
        long Id { get; }

        /// <summary>The user-facing name. May change at any time; never an identifier.</summary>
        string Name { get; }

        DeviceKind Kind { get; }

        /// <summary>
        /// Live state in the user's own words - "closed", "opening…", "62%".
        /// </summary>
        /// <remarks>
        /// The host words this because only the host knows what the thing is. Core
        /// shows it verbatim, which is what lets the ship browser double as an
        /// instrument panel without core knowing a door from a fuel tank.
        /// </remarks>
        string Status { get; }

        /// <summary>Whether this device is doing something right now.</summary>
        bool IsActive { get; }

        /// <summary>
        /// Current value of a named boolean signal, or false if this device does not
        /// have one by that name.
        /// </summary>
        bool GetSignal(string signalId);
    }
}
