using System;
using VRage;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>Makes the words a mod gives Sprocket into what Sprocket asks each time it shows them, so a translated text follows the player's language.</summary>
    public static class SprocketText
    {
        /// <summary>The plain text <paramref name="text" />, the same in every language.</summary>
        public static Func<string> Of(string text)
        {
            return () => text;
        }

        /// <summary>The text <paramref name="key" /> names in the mod's own texts, in the player's language.</summary>
        public static Func<string> Of(MyStringId key)
        {
            return () => MyTexts.GetString(key);
        }
    }
}
