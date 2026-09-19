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
    public sealed class Group : Piece
    {
        [XmlAttribute("kind")]
        public string Sort;

        [XmlElement("parameter")]
        public List<Parameter> Parameters = new List<Parameter>();

        public override int Span
        {
            get { return Parameters.Count; }
        }

        public override string Kind
        {
            get { return Sort ?? ""; }
        }
    }
}
