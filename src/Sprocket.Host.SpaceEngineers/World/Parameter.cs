using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What somebody says one of an action's parameters is.
    /// </summary>
    /// <remarks>
    /// The game declares how many parameters an action takes and nothing about any of them, so this
    /// is the only place a name or a note about one can come from.
    /// <para>
    /// One of these is one parameter, always. Several that are one thing to a person go in a
    /// <see cref="Group" />, which is what keeps "every parameter is described once" true and
    /// visible.
    /// </para>
    /// <para>
    /// Public fields because the game's own serialiser reads it out of a file a mod ships.
    /// </para>
    /// </remarks>
    public sealed class Parameter : Piece
    {
        /// <summary>What to call it, or empty to leave it numbered.</summary>
        [XmlAttribute("name")]
        public string Name;

        /// <summary>
        /// What it is for, at length, or empty.
        /// </summary>
        /// <remarks>
        /// The element's own text rather than an attribute, so it can be a sentence or several
        /// without the file turning into one long line.
        /// </remarks>
        [XmlText]
        public string Note;

        /// <summary>
        /// The least it may be, and the most, or empty where nobody has said.
        /// </summary>
        /// <remarks>
        /// Text rather than numbers, because absent and zero are different things and a parameter
        /// may one day be bounded by something that is not a number at all. What it buys is a box
        /// that says what it will take before anybody types into it, and refuses what it will not -
        /// a colour channel is 0 to 255 and nothing about the action says so.
        /// </remarks>
        [XmlAttribute("min")]
        public string Least;

        [XmlAttribute("max")]
        public string Most;

        /// <summary>One. A parameter is one parameter; see <see cref="Group" /> for the other case.</summary>
        public override int Span
        {
            get { return 1; }
        }

        /// <summary>Nothing of its own. A kind belongs to a group, because a kind spans parameters.</summary>
        public override string Kind
        {
            get { return ""; }
        }
    }
}
