using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// One block an act reaches: what it does there, its values as that takes them, what the action makes of them, the
    /// order made ready where the game does it, and what the action remembers of the block.
    /// </summary>
    public sealed class ActTarget
    {
        /// <summary>The block.</summary>
        public GridEntity Entity;

        /// <summary>What it does there.</summary>
        public EntityAction Action;

        /// <summary>The number of the facet a member of a type the host provides acts on, from 1, or 0 for a member of the block.</summary>
        public int Facet;

        /// <summary>Its values, as what it does takes them.</summary>
        public IList<Literal> Arguments;

        /// <summary>What the action makes of the values, or null.</summary>
        public BoundAction Bound;

        /// <summary>The order made ready where the game does it.</summary>
        public BlockOrder Order;

        /// <summary>Whether the block is listened to for getting where the action leads, which it announces.</summary>
        public bool Heard;

        /// <summary>What is remembered of the block, kept in what the world saves while its act is waited on.</summary>
        public ActMemory Kept;

        /// <summary>What the action remembers of the block between looks, or null.</summary>
        public ReaderMemory Seen => Kept.Seen;

        /// <summary>Whether the block has said it is done with what it was told, short of what the action waits for.</summary>
        public bool Done
        {
            get { return Kept.Done; }
            set { Kept.Done = value; }
        }
    }
}
