using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>A static large grid a test builds into the world from blocks it names, and the blocks on it.</summary>
    public sealed class BuiltGrid
    {
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();

        /// <summary>A grid at <paramref name="at" /> holding <paramref name="blocks" />, each at the cell its Min gives it.</summary>
        public BuiltGrid(Vector3D at, params MyObjectBuilder_CubeBlock[] blocks)
        {
            var grid = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            grid.GridSizeEnum = MyCubeSize.Large;
            grid.IsStatic = true;
            grid.PersistentFlags = MyPersistentEntityFlags2.InScene;
            grid.PositionAndOrientation = new MyPositionAndOrientation(at, Vector3.Forward, Vector3.Up);
            grid.CubeBlocks.AddRange(blocks);
            var built = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);
            built.GetBlocks(_blocks);
        }

        /// <summary>The first block on the grid that is a <typeparamref name="T" />, or null.</summary>
        public T Find<T>() where T : class
        {
            foreach (var block in _blocks)
            {
                var found = block.FatBlock as T;
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>The block on the grid at <paramref name="cell" /> if it is a <typeparamref name="T" />, or null.</summary>
        public T At<T>(Vector3I cell) where T : class
        {
            foreach (var block in _blocks)
            {
                if (block.Position == cell)
                    return block.FatBlock as T;
            }

            return null;
        }

        /// <summary>A block of <typeparamref name="T" /> and <paramref name="subtype" /> at <paramref name="cell" />, switched on.</summary>
        public static T Block<T>(string subtype, Vector3I cell) where T : MyObjectBuilder_FunctionalBlock, new()
        {
            var block = MyObjectBuilderSerializer.CreateNewObject<T>(subtype);
            block.Min = cell;
            block.Enabled = true;
            return block;
        }

        /// <summary>A large battery at <paramref name="cell" />, charged full and switched on.</summary>
        public static MyObjectBuilder_BatteryBlock Battery(Vector3I cell)
        {
            var battery = Block<MyObjectBuilder_BatteryBlock>("LargeBlockBatteryBlock", cell);
            battery.CurrentStoredPower = 3;
            return battery;
        }
    }
}
