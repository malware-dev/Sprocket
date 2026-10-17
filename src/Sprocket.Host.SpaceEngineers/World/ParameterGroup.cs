using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Several of an action's parameters that are one thing to a person.
    /// </summary>
    /// <remarks>Read by the game's XML serializer from a file a mod ships, so its names are a persisted format.</remarks>
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
