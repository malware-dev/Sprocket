using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something a system needs a construct to provide, and what may answer it.
    /// </summary>
    /// <remarks>
    /// The parts are the system's vocabulary: its sequences act on <c>inner</c>, and which block that
    /// is on a given ship is one of that system's business. A part nothing acts on is left out when the
    /// program is written, and so is every instance's answer to it.
    /// </remarks>
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

        /// <summary>Whether this part is a trigger, answered by toolbar slots on its blocks rather than by the blocks alone.</summary>
        public bool IsSignal { get; set; }

        /// <summary>
        /// The type of block that may fill this part, or empty for any block.
        /// </summary>
        /// <remarks>
        /// A type id as the game writes it with the <c>MyObjectBuilder_</c> prefix dropped, which takes that type and every type
        /// the game builds on it, so <c>LightingBlock</c> takes interior lights and spotlights alike; or a <c>Type/Subtype</c>
        /// where the author meant one exact block.
        /// </remarks>
        public string Expects { get; set; }

        /// <summary>
        /// What somebody wrote about this part, in their own words, or empty; never its name.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is what a player setting the system up reads to know
        /// which of their blocks to point at, so it is part of the model and round-trips like a name.
        /// </remarks>
        public string Annotation { get; set; }

        /// <summary>Whether a block whose kind stands in <paramref name="lineage" />, nearest first as a kind's lineage has it, may fill this part.</summary>
        public bool Fits(IList<string> lineage)
        {
            if (Expects.Length == 0)
                return true;

            for (var i = 0; i < lineage.Count; i++)
            {
                if (string.Equals(lineage[i], Expects, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The nearest type every one of <paramref name="lineages" /> stands in, each nearest first as a kind's lineage has it, or
        /// empty for any block where they share none; one with nothing in it, a block no longer there, says nothing.
        /// </summary>
        /// <remarks>A type and never one exact block, so a slot made from blocks takes blocks of another size or style too.</remarks>
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
