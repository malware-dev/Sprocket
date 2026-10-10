using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A micro emitter: a pulse to fire now or after a countdown, once it has charged.</summary>
    /// <remarks>
    /// The game gives it no interface of its own: to a mod it is a functional block, and its discharge and countdown come
    /// from the pulse component its definition gives it, which the game finds by that component's mod-side interface.
    /// </remarks>
    public sealed class MicroEmitter : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyEmpEntityComponent pulse;
            return block is IMyFunctionalBlock && block.Components.TryGet(out pulse);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Vetting(false, "Discharge", "StartCountdown", "StopCountdown")
                .Vetting(true, "Countdown");
        }
    }
}
