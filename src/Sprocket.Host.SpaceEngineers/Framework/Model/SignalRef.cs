using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A variable holding a signal: the one sent from a slot on the toolbars of the blocks other variables hold.</summary>
    /// <remarks>Written <c>let button = sprocket.toolbar(1, 1, 4) on [ panel1 ]</c>.</remarks>
    public sealed class SignalRef
    {
        readonly List<string> _blocks = new List<string>();

        /// <summary>The variable <paramref name="name" />, holding slot <paramref name="at" /> on the blocks <paramref name="blocks" /> name.</summary>
        public SignalRef(string name, IList<string> blocks, ToolbarSlot at)
        {
            Name = name;
            At = at;
            for (var i = 0; i < blocks.Count; i++)
                _blocks.Add(blocks[i]);
        }

        /// <summary>What the program calls this signal.</summary>
        public string Name { get; private set; }

        /// <summary>The variables holding the blocks the slot is on.</summary>
        public IList<string> Blocks => _blocks;

        /// <summary>Which slot on those blocks' toolbars.</summary>
        public ToolbarSlot At { get; private set; }

        /// <summary>What somebody wrote above it, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>Whether it holds slot <paramref name="at" /> on the block <paramref name="block" /> names.</summary>
        public bool Holds(string block, ToolbarSlot at)
        {
            return At.Same(at) && _blocks.Contains(block);
        }

        /// <summary>Whether it holds slot <paramref name="at" /> on the block <paramref name="block" /> names, and on no other.</summary>
        public bool HoldsOnly(string block, ToolbarSlot at)
        {
            return At.Same(at) && _blocks.Count == 1 && _blocks[0] == block;
        }
    }
}
