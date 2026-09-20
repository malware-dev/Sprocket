namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The person at the screen, as far as Sprocket needs to know them.</summary>
    static class Player
    {
        /// <summary>The private use area, where a font is free to draw whatever it likes.</summary>
        /// <remarks>
        /// Space Engineers keeps its own marks here and draws one in front of a player's display name,
        /// so <c>MyName</c> answers with it attached. See <see cref="Plain" />.
        /// </remarks>
        const char MarksFrom = '';

        const char MarksTo = '';

        /// <summary>
        /// A display name without the mark the game draws in front of it.
        /// </summary>
        /// <remarks>
        /// <b>Only from the front, and only the game's own marks.</b> What the game prefixes is not part
        /// of anybody's name and means nothing in another font, so it has no business becoming the
        /// namespace a system is written under. Everything after it is what the player typed, and people
        /// put all sorts into a name - so the rest is left exactly as it arrived, whatever it holds.
        /// </remarks>
        public static string Plain(string shown)
        {
            if (string.IsNullOrEmpty(shown))
                return shown;

            var at = 0;
            while (at < shown.Length && shown[at] >= MarksFrom && shown[at] <= MarksTo)
                at++;

            return at == 0 ? shown : shown.Substring(at);
        }
    }
}
