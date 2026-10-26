using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lumel.Sim;

namespace Sprocket.Sim.Content
{
    /// <summary>
    /// Takes what the simulator needs out of the game, once, on a machine that has it.
    /// </summary>
    /// <remarks>
    /// Named for what it is rather than hidden inside the first thing that happens to want a picture.
    /// Extraction reads somebody else's install and writes outside the repository, and both of those are
    /// worth being able to point at, ask for by name, and see the result of - rather than having them
    /// happen quietly the first time a screen is drawn.
    /// </remarks>
    public static class Extract
    {
        static readonly string[][] Wanted =
        {
            new[] { "Fonts/white/FontDataPA-0.dds", "the letterforms themselves" },
            new[] { "Textures/GUI/MouseCursor.dds", "the pointer" },
            new[] { "Textures/GUI/MouseCursorHand.dds", "the pointer, over something that answers" }
        };

        /// <summary>Extracts everything, or just the one path if one was named.</summary>
        public static string Run(string only)
        {
            var report = new StringBuilder();

            if (!GameContent.HasGame)
            {
                report.AppendLine("no game on this machine - nothing to extract from");
                report.AppendLine("the cache is what stands in for it here:");
                report.AppendLine("  " + GameContent.CacheDirectory);
                return report.ToString();
            }

            report.AppendLine("from  " + GameContent.ContentFolder);
            report.AppendLine("into  " + GameContent.CacheDirectory);
            report.AppendLine();

            if (!string.IsNullOrEmpty(only))
            {
                One(report, only, "asked for");
                return report.ToString();
            }

            foreach (var entry in Wanted)
                One(report, entry[0], entry[1]);

            foreach (KeyValuePair<string, string> art in BlockArt.All)
                One(report, art.Value, "what a " + art.Key + " looks like");

            return report.ToString();
        }

        static void One(StringBuilder report, string relativePath, string why)
        {
            report.AppendLine(relativePath);
            report.AppendLine("    " + why);

            if (!GameImages.CanExtract(relativePath))
            {
                report.AppendLine("    NOT IN THIS INSTALL - skipped");
                report.AppendLine();
                return;
            }

            // Removed first, so asking again is how a bad extraction gets fixed rather than something
            // that quietly answers with the same wrong file.
            string cached = GameImages.CachedPath(relativePath);
            if (File.Exists(cached))
                File.Delete(cached);

            string produced;
            try
            {
                produced = GameImages.Resolve(relativePath);
            }
            catch (Exception error)
            {
                report.AppendLine("    FAILED - " + error.Message);
                report.AppendLine();
                return;
            }

            if (produced == null)
            {
                report.AppendLine("    FAILED - nothing came out");
                report.AppendLine();
                return;
            }

            var info = new FileInfo(produced);
            report.AppendLine("    -> " + produced + "   " + info.Length + " bytes");
            report.AppendLine();
        }
    }
}
