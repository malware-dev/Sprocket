namespace Phosphor
{
    /// <summary>
    /// Copied text, for as long as the game is running.
    /// <para>
    /// Not the operating system's clipboard, because the mod API does not offer one. That means text cannot
    /// be pasted in from outside the game, which is a real limitation and worth knowing about rather than
    /// discovering. Everything goes through these two methods so that if a route out ever appears, it
    /// appears here and nowhere else.
    /// </para>
    /// </summary>
    public static class Clipboard
    {
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
