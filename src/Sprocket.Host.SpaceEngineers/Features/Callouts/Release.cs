namespace Sprocket.Host.SpaceEngineers.Features.Callouts
{
    /// <summary>The number of the release in the works, which each callout is made to arrive in.</summary>
    public static class Release
    {
        /// <summary>The number of the release in the works; raised by one after each successful publish.</summary>
        /// <remarks>Persisted: a player's file records the highest number whose callouts were all dismissed.</remarks>
        public const int Number = 1;
    }
}
