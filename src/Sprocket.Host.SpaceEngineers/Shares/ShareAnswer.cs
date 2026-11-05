namespace Sprocket.Host.SpaceEngineers.Shares
{
    /// <summary>What the server said to a blueprint offered for sharing.</summary>
    public enum ShareAnswer
    {
        /// <summary>It is shared.</summary>
        Shared,

        /// <summary>The world's settings allow no sharing.</summary>
        Closed,

        /// <summary>An admin has stopped this player sharing.</summary>
        Barred,

        /// <summary>This player already shares as many blueprints as the world allows.</summary>
        AtLimit,

        /// <summary>The blueprint would not read, or is longer than a share may be.</summary>
        Unreadable
    }
}
