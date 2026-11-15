using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A gravity generator, box or sphere: its pull set or reversed, and the size of its field.</summary>
    /// <remarks>The pull is in meters per second squared, and below 0 it pulls the other way.</remarks>
    public sealed class GravityGenerator : Capability
    {
        static readonly OwnAction Reverses = OwnAction.Of<IMyGravityGeneratorBase>(generator =>
            generator.GravityAcceleration = -generator.GravityAcceleration);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGravityGeneratorBase;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("reverse", "reverse", Reverses, (string)null).In(Category.Other)
                .Renaming("Gravity", true, "acceleration")
                .Measuring("Gravity", " m/s2")
                .Hiding("SetGravityAcceleration");

            if (block is IMyGravityGenerator)
            {
                kind.Renaming("Width", true, "width")
                    .Measuring("Width", " m")
                    .Renaming("Height", true, "height")
                    .Measuring("Height", " m")
                    .Renaming("Depth", true, "depth")
                    .Measuring("Depth", " m");
            }

            if (block is IMyGravityGeneratorSphere)
                kind.Renaming("Radius", true, "radius")
                    .Measuring("Radius", " m");
        }
    }
}
