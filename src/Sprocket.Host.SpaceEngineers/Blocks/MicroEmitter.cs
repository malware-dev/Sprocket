using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A micro emitter: a pulse to fire now or after a countdown, once it has charged.</summary>
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
                .Vetting(true, "Countdown")
                .Measuring("Countdown", " s");
        }
    }
}
