using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>Reads the parts of the dictionaries a mod sends Sprocket over its mod support.</summary>
    public static class ModWire
    {
        /// <summary>The part <paramref name="key" /> of <paramref name="said" /> as a <typeparamref name="T" />, or the default where it is missing or of another type.</summary>
        public static T Part<T>(Dictionary<string, object> said, string key)
        {
            object part;
            return said != null && said.TryGetValue(key, out part) && part is T ? (T)part : default(T);
        }

        /// <summary>What <paramref name="text" /> says now, or empty where the mod gave none or its text fails.</summary>
        /// <remarks>A mod's text is asked each time it is shown, so it follows the player's language.</remarks>
        public static string Said(System.Func<string> text)
        {
            if (text == null)
                return "";

            try
            {
                return text() ?? "";
            }
            catch (System.Exception)
            {
                return "";
            }
        }
    }
}
