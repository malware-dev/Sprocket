namespace Sprocket.GameTests
{
    /// <summary>How often the dedicated server calls a plugin, which is what every wait in a game test counts in.</summary>
    public static class PluginUpdates
    {
        /// <summary>The plugin updates in one second of game time.</summary>
        /// <remarks>The server calls a plugin every second gameplay frame: 90 calls in 180 frames over 3 s, measured on the dedicated server.</remarks>
        public const int PerSecond = 30;
    }
}
