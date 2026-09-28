using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something a system needs a construct to provide, and what may answer it.
    /// </summary>
    /// <remarks>
    /// The parts are the system's vocabulary: its sequences act on <c>inner</c>, and which block that
    /// is on a given ship is one of that system's business. A part is kept whether or not anything
    /// currently acts on it, because deleting the last step that mentioned a door must not quietly
    /// discard what its author wrote about it. The exception is a part the editor took for a picked
    /// block that nobody has written anything about: see <see cref="Kept" />.
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

        /// <summary>Whether the editor took this part for a block that was picked, rather than it being declared.</summary>
        public bool Picked { get; set; }

        /// <summary>Whether a construct may leave this part unfilled.</summary>
        public bool Optional { get; set; }

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

        /// <summary>
        /// Whether this part is saved, given whether anything uses it.
        /// </summary>
        /// <remarks>
        /// A part the editor took for a block, which nothing uses and nobody has written about, is what
        /// a deleted step leaves behind, and nothing is lost by letting it go.
        /// </remarks>
        public bool Kept(bool used)
        {
            return used || !Picked || Annotation.Length > 0;
        }

        /// <summary>Calls the part <paramref name="id" />, and nothing else: <see cref="AutomationSystem.RenamePart" /> renames what names it.</summary>
        public void Rename(string id)
        {
            Id = id;
        }
    }
}
