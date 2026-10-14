using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A parachute: how thick the air is, and the members of its terminal said in Sprocket's words.</summary>
    /// <remarks>
    /// Shaped like a door without being one, so the door does not claim it. Its open switch is the deploy request, which
    /// reads open from the moment a deploy is asked for whether or not the canopy is out.
    /// </remarks>
    public sealed class Parachute : Capability
    {
        const string Unheard = "A parachute raises nothing as the air around it changes.";

        static readonly Being Air = Being.Number<IMyParachute>(parachute => parachute.Atmosphere);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyParachute;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("air-density", "air density", Air).Looked(Unheard)
                .Renaming("Open_Off", false, "close the hatch")
                .Renaming("Open", false, "toggle deploy")
                .Renaming("AutoDeploy", true, "deploy automatically")
                .Renaming("AutoDeploy", false, "toggle deploy automatically")
                .Renaming("AutoDeployHeight", true, "deploy altitude")
                .Renaming("AnyoneCanUse", true, "anyone can use")
                .Renaming("AnyoneCanUse", false, "toggle anyone can use");
        }
    }
}
