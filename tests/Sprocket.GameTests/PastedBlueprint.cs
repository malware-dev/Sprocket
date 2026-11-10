using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>A blueprint file pasted into the world as a player pastes one, with new ids for every grid and block.</summary>
    public sealed class PastedBlueprint
    {
        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();

        /// <summary>Pastes the blueprint at <paramref name="path" />, a bp.sbc as the game writes one, moved by <paramref name="offset" /> so pasted builds stand apart.</summary>
        public PastedBlueprint(string path, Vector3D offset)
        {
            var definitions = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_Definitions>(File.ReadAllText(path));
            var grids = new List<MyObjectBuilder_EntityBase>(definitions.ShipBlueprints[0].CubeGrids);
            MyAPIGateway.Entities.RemapObjectBuilderCollection(grids);
            foreach (var grid in grids)
            {
                var placed = grid.PositionAndOrientation ?? new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
                placed.Position = placed.Position + offset;
                grid.PositionAndOrientation = placed;
                _grids.Add((IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
            }
        }

        /// <summary>The terminal block named <paramref name="name" /> on any of the pasted grids, or null where none is.</summary>
        public IMyTerminalBlock Find(string name)
        {
            var blocks = new List<IMySlimBlock>();
            foreach (var grid in _grids)
            {
                blocks.Clear();
                grid.GetBlocks(blocks, block => block.FatBlock is IMyTerminalBlock);
                foreach (var block in blocks)
                {
                    var terminal = (IMyTerminalBlock)block.FatBlock;
                    if (terminal.CustomName == name)
                        return terminal;
                }
            }

            return null;
        }
    }
}
