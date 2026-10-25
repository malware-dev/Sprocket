using System;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>An installation a join brought into another program, by its logic system and number there and where it came from.</summary>
    public sealed class InstanceMove
    {
        /// <summary>The identity of the logic system it came from.</summary>
        public Guid FromSystem { get; set; }

        /// <summary>Its number in the logic system it came from.</summary>
        public int FromNumber { get; set; }

        /// <summary>The identity of the logic system it is in now.</summary>
        public Guid System { get; set; }

        /// <summary>Its number in the logic system it is in now.</summary>
        public int Number { get; set; }
    }
}
