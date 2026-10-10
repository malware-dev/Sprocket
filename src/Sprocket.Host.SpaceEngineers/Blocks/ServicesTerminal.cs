using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A services terminal: repair and salvage offered to visitors, which anyone may be allowed to use.</summary>
    /// <remarks>
    /// The game gives it no interface of its own: to a mod it is a functional block with a repair service component, which
    /// the game finds by that component's mod-side interface. Its two range sliders are shown only on a terminal whose
    /// services work away from a station, and the game's own terminal's do not (EntityComponents.sbc, WorksOutsideStation),
    /// so a player never sees them and Sprocket does not vouch for them.
    /// </remarks>
    public sealed class ServicesTerminal : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyRepairServiceComponent repair;
            return block is IMyFunctionalBlock && block.Components.TryGet(out repair);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Vetting(true, "AnyoneCanUse").Vetting(false, "AnyoneCanUse");
        }
    }
}
