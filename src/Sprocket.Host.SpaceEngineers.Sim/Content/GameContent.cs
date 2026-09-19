
using System;
using System.IO;
using System.Collections.Generic;
namespace Sprocket.Sim.Content
{
    /// <summary>
    /// Finds the game's content on a machine that has it, and keeps a copy of the little we need on one
    /// that does not.
    /// </summary>
    /// <remarks>
    /// The split matters more than it looks. Everything the simulator needs from Space Engineers is small,
    /// static and derived from files rather than from a running game, so it can be fetched once here and
    /// read anywhere afterwards. That is what lets this be developed on a machine the game is not installed
    /// on, and it is the same shape the block catalogue will take when we get to it: a tool that runs where
    /// the game is, producing a file that stands in where it is not.
    /// <para>
    /// The cache lives outside the repository on purpose. It is Keen's content, not ours to redistribute.
    /// </para>
    /// </remarks>
    public static class GameContent
    {
        /// <summary>Point this at a Content folder to override every guess below.</summary>
        public const string OverrideVariable = "SPROCKET_SE_CONTENT";

        /// <summary>Where fetched files are kept, so a machine without the game still has them.</summary>
        public static string CacheDirectory
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(root, "Sprocket", "SimContent");
            }
        }

        /// <summary>
        /// Hands back a path to <paramref name="relativePath" />, taking it from the game if the game is
        /// here and caching it on the way past, or from the cache if it is not. Null when neither has it.
        /// </summary>
        public static string Resolve(string relativePath)
        {
            string cached = Path.Combine(CacheDirectory, relativePath);

            string content = FindContentFolder();
            if (content != null)
            {
                string live = Path.Combine(content, relativePath);
                if (File.Exists(live))
                {
                    TryCache(live, cached);
                    return live;
                }
            }

            return File.Exists(cached) ? cached : null;
        }

        /// <summary>Whether what we are reading came from the game itself rather than from the cache.</summary>
        public static bool HasGame => FindContentFolder() != null;

        /// <summary>Where the game's content was found, or null on a machine that does not have it.</summary>
        public static string ContentFolder => FindContentFolder();

        static string _contentFolder;
        static bool _searched;

        static string FindContentFolder()
        {
            if (_searched)
                return _contentFolder;

            _searched = true;
            _contentFolder = Search();
            return _contentFolder;
        }

        static string Search()
        {
            string overridden = Environment.GetEnvironmentVariable(OverrideVariable);
            if (!string.IsNullOrEmpty(overridden) && Directory.Exists(overridden))
                return overridden;

            foreach (string library in SteamLibraries())
            {
                // The Mod SDK carries the same fonts and is a smaller install, so it is worth accepting
                // either. Whichever is present first is as good as the other for what we take.
                string[] candidates =
                {
                    Path.Combine(library, "steamapps", "common", "SpaceEngineers", "Content"),
                    Path.Combine(library, "steamapps", "common", "SpaceEngineersModSDK", "OriginalContent")
                };

                foreach (string candidate in candidates)
                    if (Directory.Exists(candidate))
                        return candidate;
            }

            return null;
        }

        static IEnumerable<string> SteamLibraries()
        {
            var roots = new List<string>();

            foreach (Environment.SpecialFolder folder in new[]
                     {
                         Environment.SpecialFolder.ProgramFilesX86,
                         Environment.SpecialFolder.ProgramFiles
                     })
            {
                string steam = Path.Combine(Environment.GetFolderPath(folder), "Steam");
                if (Directory.Exists(steam))
                    roots.Add(steam);
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string steam in roots)
            {
                if (seen.Add(steam))
                    yield return steam;

                // libraryfolders.vdf lists the other drives Steam was told about. Read as plain text
                // rather than parsed: we want the quoted paths and nothing else from it.
                string manifest = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(manifest))
                    continue;

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(manifest);
                }
                catch (IOException)
                {
                    continue;
                }

                foreach (string line in lines)
                {
                    int marker = line.IndexOf("\"path\"", StringComparison.OrdinalIgnoreCase);
                    if (marker < 0)
                        continue;

                    int open = line.IndexOf('"', marker + 6);
                    int close = open < 0 ? -1 : line.IndexOf('"', open + 1);
                    if (open < 0 || close < 0)
                        continue;

                    string path = line.Substring(open + 1, close - open - 1).Replace("\\\\", "\\");
                    if (Directory.Exists(path) && seen.Add(path))
                        yield return path;
                }
            }
        }

        static void TryCache(string source, string destination)
        {
            try
            {
                if (File.Exists(destination)
                    && File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(source))
                    return;

                string folder = Path.GetDirectoryName(destination);
                if (folder != null)
                    Directory.CreateDirectory(folder);

                File.Copy(source, destination, true);
            }
            catch (IOException)
            {
                // Caching is a convenience for another machine, never a requirement for this one.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
