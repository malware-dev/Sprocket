using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A parachute, read for how thick the air is.</summary>
    /// <remarks>Its open switch reads open from the moment a deploy is asked for, canopy out or not.</remarks>
    public sealed class Parachute : Capability
    {
        const string Unheard = "A parachute raises nothing as the air around it changes.";

        static readonly OwnValue Air = OwnValue.Number<IMyParachute>(parachute => parachute.Atmosphere);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyParachute;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("air-density", "air density", Air).Between("0", "1").Measured(" %", 100).Looked(Unheard).In(Category.Flight)
                .Renaming("Open_Off", false, "close the hatch")
                .Renaming("Open", false, "toggle deploy")
                .Renaming("AutoDeploy", true, "deploy automatically")
                .Renaming("AutoDeploy", false, "toggle deploy automatically")
                .Renaming("AutoDeployHeight", true, "deploy altitude")
                .Measuring("AutoDeployHeight", " m")
                .Renaming("AnyoneCanUse", true, "anyone can use")
                .Renaming("AnyoneCanUse", false, "toggle anyone can use");
        }
    }
}
