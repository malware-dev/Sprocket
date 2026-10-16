using System;
using System.IO;
using System.Text;

namespace Sprocket.Sim.Content
{
    /// <summary>
    /// Says which machine this is and whether the game is on it.
    /// </summary>
    /// <remarks>
    /// Because the answer changes between machines and nothing else in the repository can say so. Half
    /// the work here - the real letterforms, the block artwork, the game's own chrome to sample - is only
    /// possible where Space Engineers is installed, and the difference between "this target has no
    /// artwork" and "this machine cannot see the artwork" is one somebody has to be able to ask about
    /// rather than infer from a screenshot looking wrong.
    /// <para>
    /// A command rather than a note written down somewhere, for the same reason a test beats a comment:
    /// a note about where the game lives is wrong the moment somebody moves it, and this cannot be.
    /// </para>
    /// </remarks>
    public static class ContentReport
    {
        /// <summary>Everything worth knowing about what this machine can supply, as plain text.</summary>
        public static string Render()
        {
            var report = new StringBuilder();

            report.AppendLine("machine    " + SafeMachineName());
            report.AppendLine("game       " + (GameContent.HasGame ? "yes" : "no"));

            string content = GameContent.ContentFolder;
            report.AppendLine("content    " + (content ?? "(not found)"));

            string overridden = Environment.GetEnvironmentVariable(GameContent.OverrideVariable);
            report.AppendLine("override   " + (string.IsNullOrEmpty(overridden)
                ? "(unset - " + GameContent.OverrideVariable + " would win over the search)"
                : overridden));

            report.AppendLine("cache      " + GameContent.CacheDirectory);

            // What is actually in the cache, because that is what a machine without the game falls back
            // on and "the cache exists" is not the same claim as "the cache has what is needed".
            report.AppendLine();
            report.AppendLine("cached files");

            if (!Directory.Exists(GameContent.CacheDirectory))
            {
                report.AppendLine("  (none)");
                return report.ToString();
            }

            string[] files = Directory.GetFiles(GameContent.CacheDirectory, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
                report.AppendLine("  (none)");

            foreach (string file in files)
            {
                string relative = file.Substring(GameContent.CacheDirectory.Length).TrimStart(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                report.AppendLine("  " + relative + "   " + new FileInfo(file).Length + " bytes");
            }

            return report.ToString();
        }

        static string SafeMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch (Exception)
            {
                return "(unknown)";
            }
        }
    }
}
