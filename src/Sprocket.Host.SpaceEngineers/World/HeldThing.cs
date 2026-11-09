using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One thing a slot holds in an installation: a block, or one facet of a block.</summary>
    public struct HeldThing
    {
        /// <summary>The block.</summary>
        public IMyTerminalBlock Block;

        /// <summary>The number of the facet, from 1, or 0 where the slot holds the block itself.</summary>
        public int Facet;
    }
}
