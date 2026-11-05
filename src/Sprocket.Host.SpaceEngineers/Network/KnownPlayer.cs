namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>A player of this world, as the server tells a client of them.</summary>
    public sealed class KnownPlayer
    {
        /// <summary>Their identity.</summary>
        public long Identity;

        /// <summary>Their name.</summary>
        public string Name = "";

        /// <summary>Whether the player being told has blocked them.</summary>
        public bool IsBlocked;

        /// <summary>Whether an admin has stopped them sharing.</summary>
        public bool IsBarred;
    }
}
