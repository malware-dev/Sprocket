using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something a system needs a construct to provide, and what may answer it.
    /// </summary>
    /// <remarks>A part nothing acts on is left out when the program is written, and so is every instance's answer to it.</remarks>
    public sealed class Part
    {
        /// <summary>A required slot called <paramref name="id" />, expecting any block.</summary>
        public Part(string id)
        {
            Id = id;
            Annotation = "";
            Expects = "";
        }

        /// <summary>The part's name, which is the slot's name wherever it is shown.</summary>
        public string Id { get; private set; }

        /// <summary>Whether a construct may leave this part unfilled.</summary>
        public bool Optional { get; set; }

        /// <summary>For a signal slot, the few words shown with each toolbar slot that answers it, or empty for its name.</summary>
        public string Short { get; set; } = "";

        /// <summary>The category of its system the slot is shown under on the installation panel, or empty for none.</summary>
        public string Category { get; set; } = "";

        /// <summary>Whether this part is a trigger, answered by toolbar slots on its blocks rather than by the blocks alone.</summary>
        public bool IsSignal { get; set; }

        /// <summary>
        /// The id of the type the host provides that this part holds, as a part expecting it names it, <c>ext.screen</c>; or
        /// empty for a part holding blocks or a signal.
        /// </summary>
        public string Holds { get; set; } = "";

        /// <summary>
        /// The type of block that may fill this part, or empty for any block.
        /// </summary>
        /// <remarks>
        /// A game type id without its <c>MyObjectBuilder_</c> prefix, taking every type the game builds on it; or a
        /// <c>Type/Subtype</c> for one exact block.
        /// </remarks>
        public string Expects { get; set; }

        /// <summary>
        /// What somebody wrote about this part, in their own words, or empty; never its name.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>Whether a block whose kind stands in <paramref name="lineage" />, nearest first as a kind's lineage has it, may fill this part.</summary>
        public bool Fits(IList<string> lineage) => Takes(Expects, lineage);

        /// <summary>Whether a slot expecting <paramref name="expects" />, empty for any block, takes a block whose kind stands in <paramref name="lineage" />, nearest first.</summary>
        public static bool Takes(string expects, IList<string> lineage)
        {
            if (expects.Length == 0)
                return true;

            foreach (var type in lineage)
            {
                if (string.Equals(type, expects, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The nearest type every one of <paramref name="lineages" /> stands in, each nearest first as a kind's lineage has it, or
        /// empty for any block where they share none; one with nothing in it, a block no longer there, says nothing.
        /// </summary>
        public static string Nearest(IList<IList<string>> lineages)
        {
            IList<string> first = null;
            for (var i = 0; i < lineages.Count && first == null; i++)
            {
                if (lineages[i].Count > 0)
                    first = lineages[i];
            }

            if (first == null)
                return "";

            for (var t = 0; t < first.Count; t++)
            {
                if (first[t].IndexOf('/') >= 0)
                    continue;

                var shared = true;
                for (var i = 0; i < lineages.Count && shared; i++)
                    shared = lineages[i].Count == 0 || lineages[i].Contains(first[t]);

                if (shared)
                    return first[t];
            }

            return "";
        }

        /// <summary>Calls the part <paramref name="id" />, and nothing else: <see cref="AutomationSystem.RenamePart" /> renames what names it.</summary>
        public void Rename(string id)
        {
            Id = id;
        }
    }
}
