using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>The changes that carry a program written for an earlier version or draft of the format into the one this build writes.</summary>
    /// <remarks>
    /// The upgraded program is written in the current format at its next save. A finished version counts as later than all
    /// of its drafts, so a program written for one runs only the changes of the versions after it.
    /// </remarks>
    public static class FormatUpgrades
    {
        /// <summary>The oldest draft of version 1 a program can be written for and still be upgraded.</summary>
        public const int Oldest = 14;

        /// <summary>The draft a finished version is read as: later than every draft of it.</summary>
        public const int Finished = int.MaxValue;

        /// <summary>The last draft of version 1 any build wrote.</summary>
        public const int LastOfVersion1 = 28;

        // Each version's changes, in the order they arrived; immutable tables, read by every load. Version 1's are the
        // permanent upgrade into version 2.
        static readonly FormatChange[][] Versions =
        {
            new FormatChange[]
            {
                new PowerValue(),
                new DetectionsCount(),
                new ScreensOnSlots(),
                new LastSequenceAttribute(),
                new FromBlueprint(),
                new HeaderLines(),
                new OneScreenSlots(),
                new MemberSpelling(),
                new ComponentMembers(),
            },
            new FormatChange[0],
        };

        /// <summary>Whether a program written for <paramref name="draft" /> of version <paramref name="version" /> is upgraded rather than refused.</summary>
        public static bool From(int version, int draft)
        {
            if (version == 1)
                return draft >= Oldest && (draft <= LastOfVersion1 || draft == Finished);

            return version == ScriptFormat.Version && draft < ScriptFormat.Draft;
        }

        /// <summary>
        /// Rewrites <paramref name="constructs" />, read from a program written for <paramref name="draft" /> of version
        /// <paramref name="version" />, into the current form, before they are read.
        /// </summary>
        public static void Statements(int version, int draft, List<Statement> constructs)
        {
            foreach (var change in Since(version, draft))
                change.Statements(constructs);
        }

        /// <summary>
        /// Carries <paramref name="program" />, read as written for <paramref name="draft" /> of version
        /// <paramref name="version" />, into the current format, the blocks it names found in <paramref name="world" /> where
        /// there is one.
        /// </summary>
        /// <returns>What was upgraded, for the log, or empty where nothing needed it.</returns>
        public static string Program(int version, int draft, Automation program, GridWorld world)
        {
            var said = new List<string>();
            foreach (var change in Since(version, draft))
            {
                var did = change.Program(program, world);
                if (did.Length > 0)
                    said.Add(did);
            }

            return string.Join(" ", said);
        }

        /// <summary>Every change that arrived after <paramref name="draft" /> of version <paramref name="version" />, in order.</summary>
        static IEnumerable<FormatChange> Since(int version, int draft)
        {
            for (var at = version; at <= Versions.Length; at++)
            {
                foreach (var change in Versions[at - 1])
                {
                    if (at > version || change.Arrived > draft)
                        yield return change;
                }
            }
        }
    }
}
