using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One part of what somebody said an action's parameters are.
    /// </summary>
    /// <remarks>
    /// Either one parameter or a group of them, in the order the action declares them, so the two
    /// spellings live in one list and the order between them is kept. Which matters: order is the
    /// only thing the game itself says about an action's parameters.
    /// </remarks>
    [XmlInclude(typeof(Parameter))]
    [XmlInclude(typeof(Group))]
    public abstract class Piece
    {
        /// <summary>How many of the action's parameters this accounts for.</summary>
        [XmlIgnore]
        public abstract int Span { get; }

        /// <summary>
        /// What sort of thing it is, or empty for a plain value typed as it is written.
        /// </summary>
        /// <remarks>
        /// A small closed set, because each one is an editor Sprocket has to draw. Anything it does
        /// not know reads as plain values and keeps its names, so a file written against a later
        /// build still works in an older one - it loses the editor and nothing else.
        /// </remarks>
        [XmlIgnore]
        public abstract string Kind { get; }
    }
}
