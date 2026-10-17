using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Several of an action's parameters that are one thing to a person.
    /// </summary>
    /// <remarks>
    /// A colour the game takes as three numbers is one choice and three parameters. Written as a
    /// group with the three inside it, so every parameter is still described exactly once and how
    /// many are covered is plain to see rather than counted out in an attribute.
    /// <para>
    /// Public fields because the game's own serialiser reads it out of a file a mod ships.
    /// </para>
    /// </remarks>
    public sealed class ParameterGroup : ParameterPiece
    {
        /// <summary>What the parameters make up together, as the file names it: <c>color</c>, for one.</summary>
        [XmlAttribute("kind")]
        public string Sort;

        /// <summary>The parameters the group covers, in order.</summary>
        [XmlElement("parameter")]
        public List<Parameter> Parameters = new List<Parameter>();

        /// <inheritdoc />
        public override int Span => Parameters.Count;

        /// <inheritdoc />
        public override string Kind => Sort ?? "";
    }
}
