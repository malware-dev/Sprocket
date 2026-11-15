using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Sprocket.Sim.Content
{
    /// <summary>
    ///     The font mods Sprocket requires, read where Steam keeps them: in the Workshop folder of whichever Steam library
    ///     holds them, once the game has loaded a world that uses them.
    /// </summary>
    public static class WorkshopMods
    {
        /// <summary>Maltek Symbols, the symbols the editor's buttons and marks are drawn in.</summary>
        public const string Symbols = "3807241468";

        /// <summary>Maltek Mono, the editor's code face.</summary>
        public const string Mono = "3807240295";

        /// <summary>Space Engineers Font Styles, the bold, italic and bold italic cuts of the game's own face.</summary>
        public const string FontStyles = "3814731076";

        // Space Engineers' app id, which names its folder of Workshop items.
        const string Game = "244850";

        /// <summary>
        ///     The folder of the Workshop item <paramref name="id" />, called <paramref name="name" />; or a stop that says how to
        ///     get it where no Steam library holds it.
        /// </summary>
        public static string Required(string id, string name)
        {
            var folder = Folder(id);
            if (folder != null)
                return folder;

            throw new InvalidOperationException(
                $"The font mod {name} (Workshop item {id}) is not on this machine. Load a world with Sprocket in the game once, which downloads it.");
        }

        /// <summary>The folder of the Workshop item <paramref name="id" /> in the first Steam library holding it, or null.</summary>
        public static string Folder(string id)
        {
            foreach (var library in Libraries())
            {
                var folder = Path.Combine(library, "steamapps", "workshop", "content", Game, id);
                if (Directory.Exists(folder))
                    return folder;
            }

            return null;
        }

        /// <summary>Every Steam library on the machine: Steam's own folder, then each one its library list names.</summary>
        static IEnumerable<string> Libraries()
        {
            var steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (string.IsNullOrEmpty(steam))
                yield break;

            yield return steam;

            var list = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(list))
                yield break;

            foreach (Match path in Regex.Matches(File.ReadAllText(list), "\"path\"\\s+\"([^\"]+)\""))
                yield return path.Groups[1].Value.Replace(@"\\", @"\");
        }
    }
}
