using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A text panel: a block with one screen, reached as every block's screen is, through <see cref="ScreenType" />.</summary>
    /// <remarks>What is the panel's own is here: its title is left to the terminal, and its rotation is read in degrees.</remarks>
    public sealed class TextPanel : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTextPanel;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Hiding("Title")
                .Renaming("Rotate", true, "rotation")
                .Measuring("Rotate", "°", 1, 90, "270");
        }
    }
}
