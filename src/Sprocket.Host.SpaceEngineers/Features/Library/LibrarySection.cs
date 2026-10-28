namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>Which part of the library a listed blueprint stands in.</summary>
    public enum LibrarySection
    {
        /// <summary>A logic system on this construct.</summary>
        OnConstruct,

        /// <summary>A backup of a construct that lost its automation.</summary>
        BackedUp,

        /// <summary>A blueprint the player keeps for every world.</summary>
        Kept
    }
}
