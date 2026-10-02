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
        readonly List<string> _expects = new List<string>();

        public Part(string id)
        {
            Id = id;
            Annotation = "";
        }

        /// <summary>The part's name, which is the slot's name wherever it is shown.</summary>
        public string Id { get; private set; }

        /// <summary>Whether a construct may leave this part unfilled.</summary>
        public bool Optional { get; set; }

        /// <summary>Whether this part is a trigger, answered by toolbar slots on its blocks rather than by the blocks alone.</summary>
        public bool IsTrigger { get; set; }

        /// <summary>
        /// The block types that may fill this part, or none for anything.
        /// </summary>
        /// <remarks>
        /// A type id as the game writes it with the <c>MyObjectBuilder_</c> prefix dropped, and a
        /// <c>Type/Subtype</c> where the author meant one exact block. Several, because a family can
        /// span type ids: a door is <c>Door</c>, <c>AirtightSlideDoor</c> and <c>AirtightHangarDoor</c>.
        /// </remarks>
        public IList<string> Expects => _expects;

        /// <summary>
        /// What somebody wrote about this part, in their own words, or empty; never its name.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is what a player setting the system up reads to know
        /// which of their blocks to point at, so it is part of the model and round-trips like a name.
        /// </remarks>
        public string Annotation { get; set; }

        /// <summary>Calls the part <paramref name="id" />, and nothing else: <see cref="AutomationSystem.RenamePart" /> renames what names it.</summary>
        public void Rename(string id)
        {
            Id = id;
        }
    }
}
