using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One file's worth of what a mod says about its own blocks.
    /// </summary>
    /// <remarks>
    /// Broader than what it holds today on purpose: a mod saying what its actions' parameters are
    /// will want, later, to say what its blocks can be asked about and what moments they raise. That
    /// belongs in the same file, because a second file is a second name everybody has to be told.
    /// </remarks>
    [XmlRoot("sprocket-definitions")]
    public sealed class Descriptions
    {
        /// <summary>Every action described, each by its id.</summary>
        [XmlArray("actions")]
        [XmlArrayItem("action")]
        public List<Description> Actions = new List<Description>();
    }
}
