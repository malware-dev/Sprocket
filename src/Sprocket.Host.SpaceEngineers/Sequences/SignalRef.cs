namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A variable holding a signal: the one sent from a slot on the toolbar of a block another variable holds.</summary>
    /// <remarks>
    /// What answers <c>on</c> in a system, which names the construct's own signals the way it names its blocks. Written
    /// <c>let button = panel1:toolbar(1, 1, 4)</c>: the host function this host names a signal with.
    /// </remarks>
    public sealed class SignalRef
    {
        /// <summary>The variable <paramref name="name" />, holding slot <paramref name="at" /> on the block <paramref name="block" /> names.</summary>
        public SignalRef(string name, string block, ToolbarSlot at)
        {
            Name = name;
            Block = block;
            At = at;
        }

        /// <summary>What the program calls this signal.</summary>
        public string Name { get; private set; }

        /// <summary>The variable holding the block the slot is on.</summary>
        public string Block { get; private set; }

        /// <summary>Which slot on that block's toolbars.</summary>
        public ToolbarSlot At { get; private set; }

        /// <summary>What somebody wrote above it, or empty.</summary>
        public string Annotation { get; set; } = "";
    }
}
