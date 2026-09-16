using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Core.Sequences
{
    /// <summary>
    /// Something a system needs a construct to provide, and what may answer it.
    /// </summary>
    /// <remarks>
    /// The parts are the system's vocabulary: its sequences act on <c>inner</c>, and which block that
    /// is on a given ship is one of that system's business. A part is kept whether or not anything
    /// currently acts on it, because deleting the last step that mentioned a door must not quietly
    /// discard what its author wrote about it.
    /// </remarks>
    public sealed class Part
    {
        readonly List<string> _expects = new List<string>();

        public Part(string id)
        {
            Id = id;
            Annotation = "";
        }

        public string Id { get; private set; }

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
        /// What somebody wrote about this part, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is what a player fitting the system reads to know
        /// which of their blocks to point at, so it is part of the model and round-trips like a name.
        /// </remarks>
        public string Annotation { get; set; }
    }
}
