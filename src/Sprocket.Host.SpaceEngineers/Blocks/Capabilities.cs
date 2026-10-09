using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Every capability Sprocket knows, in the order a kind of block is described by them.</summary>
    /// <remarks>
    /// What a block is comes before what every block is, so a door offers "open" above "switch on": the block's own
    /// capabilities first, and the floor every block stands on after them. A block is every capability that claims it.
    /// </remarks>
    public sealed class Capabilities
    {
        readonly Capability[] _blocks =
        {
            new Door(),
            new Vent(),
            new ButtonPanel(),
            new Rotor(),
            new Piston(),
            new Battery(),
            new Tank(),
            new Speaker(),
            new Timer(),
            new Sensor(),
            new EventController(),
            new ShipController()
        };

        readonly Capability[] _floor =
        {
            new Functional()
        };

        /// <summary>What particular kinds of block are.</summary>
        public IList<Capability> Blocks => _blocks;

        /// <summary>What every block that claims it is, described after what it is in particular.</summary>
        public IList<Capability> Floor => _floor;
    }
}
