
namespace Sprocket.Host.SpaceEngineers.Input
{
    /// <summary>Copied text, for as long as the game is running.</summary>
    /// <remarks>
    /// Not the operating system's clipboard: the mod API does not offer one, so text cannot be pasted in
    /// from outside the game. Everything goes through these two methods, so if a route out ever appears
    /// it appears here and nowhere else.
    /// </remarks>
    public static class Clipboard
    {
        /// <summary>
        /// Shared deliberately, which is the one thing here that is not an instance.
        /// </summary>
        /// <remarks>
        /// Copying on one screen and pasting on another is the whole of what a clipboard is for, so this
        /// has to outlive any one of them. An instance owned by a screen would be a different feature.
        /// </remarks>
        private static string _value = string.Empty;

        public static string Get()
        {
            return _value;
        }

        public static void Set(string text)
        {
            _value = text ?? string.Empty;
        }
    }
}
