using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One part of what somebody said an action's parameters are.
    /// </summary>
    [XmlInclude(typeof(Parameter))]
    [XmlInclude(typeof(ParameterGroup))]
    public abstract class ParameterPiece
    {
        /// <summary>How many of the action's parameters this accounts for.</summary>
        [XmlIgnore]
        public abstract int Span { get; }

        /// <summary>
        /// What sort of thing it is, or empty for a plain value typed as it is written.
        /// </summary>
        [XmlIgnore]
        public abstract string Kind { get; }
    }
}
