using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// Runs a list of named scripts and writes out the frame each one ended on.
    /// </summary>
    /// <remarks>
    /// The screens held still. Every entry is a state somebody can get the editor into, and its dump is
    /// what that state is supposed to look like - so a change nobody meant shows up as a difference in a
    /// file that is already in version control, rather than as a picture somebody has to think about.
    /// <para>
    /// One process for the whole list. Each entry is a fresh device and a fresh screen, so the order
    /// entries are written in cannot change what any of them says.
    /// </para>
    /// </remarks>
    public static class Suite
    {
        private const float Width = 1920f;
        private const float Height = 1080f;

        /// <summary>Shoots every state in the list at <paramref name="listPath" /> into the folder <paramref name="into" />, and returns what it wrote.</summary>
        public static string Run(string listPath, string into)
        {
            Directory.CreateDirectory(into);

            var report = new StringBuilder();
            IList<string> lines = File.ReadAllLines(listPath);

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                if (line.Length == 0 || line[0] == '#')
                    continue;

                string[] words = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string name = words[0];

                string frame = FrameDump.Render(Width, Height, Script.Parse(words, 1));

                // Without a byte order mark and with one kind of line ending, because these files exist
                // to be compared and a difference has to mean the screen changed.
                File.WriteAllText(
                    Path.Combine(into, name + ".txt"),
                    frame.Replace(((char)13).ToString(), string.Empty),
                    new UTF8Encoding(false));

                report.Append(name.PadRight(20)).Append(Ended(frame)).AppendLine();
            }

            return report.ToString();
        }

        private static string Ended(string frame)
        {
            foreach (string line in frame.Split('\n'))
            {
                if (line.StartsWith("on screen"))
                    return line.Substring("on screen".Length).Trim();
            }

            return "";
        }
    }
}
