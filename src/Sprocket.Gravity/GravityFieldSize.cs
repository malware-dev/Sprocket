using System;
using SpaceEngineers.Game.ModAPI;
using VRageMath;

namespace Sprocket.Gravity
{
    /// <summary>How big a gravity generator's field must be to cover a box of a grid's cells.</summary>
    /// <remarks>The size runs along the generator's own right, up and back, as the game's <c>MyGravityGenerator</c> sizes its field.</remarks>
    public static class GravityFieldSize
    {
        /// <summary>The field size that reaches every cell from <paramref name="min" /> to <paramref name="max" /> of <paramref name="generator" />'s own grid.</summary>
        public static Vector3 Covering(IMyGravityGenerator generator, Vector3I min, Vector3I max)
        {
            var grid = generator.CubeGrid;
            return Covering(grid.WorldMatrix, grid.GridSize, min, max, generator.WorldMatrix,
                generator.PositionComp.LocalVolume.Center);
        }

        /// <summary>
        /// The field size that reaches every cell from <paramref name="min" /> to <paramref name="max" /> of a grid placed at
        /// <paramref name="grid" />, its cells <paramref name="cell" /> wide, for a generator placed at
        /// <paramref name="generator" /> whose field is centered on <paramref name="centre" /> in its own space.
        /// </summary>
        public static Vector3 Covering(MatrixD grid, float cell, Vector3I min, Vector3I max, MatrixD generator, Vector3 centre)
        {
            var middle = Vector3D.Transform((Vector3D)centre, generator);
            var back = MatrixD.Transpose(generator);
            var reach = Vector3D.Zero;

            for (var corner = 0; corner < 8; corner++)
            {
                var at = new Vector3D(
                    (corner & 1) == 0 ? min.X - 0.5 : max.X + 0.5,
                    (corner & 2) == 0 ? min.Y - 0.5 : max.Y + 0.5,
                    (corner & 4) == 0 ? min.Z - 0.5 : max.Z + 0.5) * cell;

                var local = Vector3D.TransformNormal(Vector3D.Transform(at, grid) - middle, back);
                reach.X = Math.Max(reach.X, Math.Abs(local.X));
                reach.Y = Math.Max(reach.Y, Math.Abs(local.Y));
                reach.Z = Math.Max(reach.Z, Math.Abs(local.Z));
            }

            return reach * 2.0;
        }
    }
}
