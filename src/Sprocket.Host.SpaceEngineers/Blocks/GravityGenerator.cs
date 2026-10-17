using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Gravity;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A gravity generator, box or sphere: its pull set or reversed, and the size of its field.</summary>
    /// <remarks>
    /// The pull is the game's own value, in metres a second squared, and below 0 it pulls the other way. A box is sized
    /// along its own width, height and depth and can be fitted to its whole grid; a sphere has a radius.
    /// </remarks>
    public sealed class GravityGenerator : Capability
    {
        static readonly OwnAction Reverses = OwnAction.Of<IMyGravityGeneratorBase>(generator =>
            generator.GravityAcceleration = -generator.GravityAcceleration);

        static readonly OwnAction Fits = OwnAction.Of<IMyGravityGenerator>(Fit);

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
                kind.WithAction("fit-to-the-grid", "fit to the grid", Fits, (string)null).In(Category.Other)
                    .Renaming("Width", true, "width")
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

        /// <summary>Sizes <paramref name="generator" />'s field to every block of its grid, as far as the generator reaches.</summary>
        /// <remarks>The game holds the field to the generator's own limits as it is set.</remarks>
        static void Fit(IMyGravityGenerator generator)
        {
            var grid = generator.CubeGrid;
            generator.FieldSize = GravityFieldSize.Covering(generator, grid.Min, grid.Max);
        }
    }
}
