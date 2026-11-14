
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// Which change in a reading counts as the event.
    /// </summary>
    public enum SignalEdge
    {
        /// <summary>The reading just became true.</summary>
        Rising,

        /// <summary>The reading just became false.</summary>
        Falling
    }
}
