namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>Copied text, for as long as the game is running.</summary>
    /// <remarks>Not the operating system's clipboard, which the mod API does not offer.</remarks>
    public static class Clipboard
    {
        /// <summary>The copied text, which has to outlive any one screen.</summary>
        static string _value = string.Empty;

        /// <summary>The copied text, or empty when nothing has been copied.</summary>
        public static string Get()
        {
            return _value;
        }

        /// <summary>Replaces the copied text with <paramref name="text" />; null empties it.</summary>
        public static void Set(string text)
        {
            _value = text ?? string.Empty;
        }
    }
}