using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>The changes that carry a program written for an earlier draft of the format into the one this build writes.</summary>
    /// <remarks>The upgraded program is written in the current format at its next save.</remarks>
    public static class FormatUpgrades
    {
        /// <summary>The oldest draft a program can be written for and still be upgraded.</summary>
        public const int Oldest = 14;

        // Format 1's changes, in the order they arrived; an immutable table, read by every load.
        static readonly FormatChange[] Version1 =
        {
            new PowerValue(),
            new DetectionsCount(),
            new ScreensOnSlots(),
            new LastSequenceAttribute(),
            new FromBlueprint(),
            new HeaderLines(),
            new OneScreenSlots(),
            new MemberSpelling(),
        };

        /// <summary>Whether a program written for <paramref name="draft" /> of version <paramref name="version" /> is upgraded rather than refused.</summary>
        public static bool From(int version, int draft)
        {
            return version == 1 && draft >= Oldest && draft < ScriptFormat.Draft;
        }

        /// <summary>Rewrites <paramref name="constructs" />, read from a program written for <paramref name="draft" />, into the current form, before they are read.</summary>
        public static void Statements(int draft, List<Statement> constructs)
        {
            foreach (var change in Version1)
            {
                if (change.Arrived > draft)
                    change.Statements(constructs);
            }
        }

        /// <summary>
        /// Carries <paramref name="program" />, read as written for <paramref name="draft" />, into the current format, the
        /// blocks it names found in <paramref name="world" /> where there is one.
        /// </summary>
        /// <returns>What was upgraded, for the log, or empty where nothing needed it.</returns>
        public static string Program(int draft, Automation program, GridWorld world)
        {
            var said = new List<string>();
            foreach (var change in Version1)
            {
                if (change.Arrived <= draft)
                    continue;

                var did = change.Program(program, world);
                if (did.Length > 0)
                    said.Add(did);
            }

            return string.Join(" ", said);
        }
    }
}
