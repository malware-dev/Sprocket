
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Whether a member can be made into a step, and where it cannot, which of two reasons.</summary>
    /// <remarks>
    /// A member that cannot is still listed in the terminal view, so that somebody looking for it finds it and learns why,
    /// rather than concluding the block does not have it.
    /// </remarks>
    public enum Readiness
    {
        /// <summary>Offered, and usable.</summary>
        Ready,

        /// <summary>Known to Sprocket, and waiting on whether it can be offered and how; listed only in the terminal view until it can.</summary>
        Pending,

        /// <summary>
        /// Known to be out of a mod's reach, so it is set in the terminal and listed only in the
        /// terminal view.
        /// </summary>
        /// <remarks>Only ever declared by something that knows the block: never concluded.</remarks>
        Unsupported
    }
}
