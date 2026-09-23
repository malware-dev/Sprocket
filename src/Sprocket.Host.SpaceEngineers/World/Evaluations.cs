using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How far each kind of block has been checked, which is declared here and nowhere else.</summary>
    /// <remarks>
    /// Keyed on the interface, like an extension, so everything shaped like a door shares the door's
    /// answer. Anything not named is pending.
    /// </remarks>
    static class Evaluations
    {
        public static Evaluation Of(IMyTerminalBlock block)
        {
            if (block is IMyDoor || block is IMyAirVent || block is IMyMotorStator || block is IMyButtonPanel
                || block is IMyPistonBase)
                return Evaluation.Partial;

            return Evaluation.Pending;
        }
    }
}
