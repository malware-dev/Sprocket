using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One file's worth of what a mod says about its own blocks.
    /// </summary>
    [XmlRoot("sprocket-definitions")]
    public sealed class Descriptions
    {
        /// <summary>Every action described, each by its id.</summary>
        [XmlArray("actions")]
        [XmlArrayItem("action")]
        public List<Description> Actions = new List<Description>();
    }
}
