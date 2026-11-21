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
    /// <summary>A large grid a test builds into the world from blocks it names, static or free to move, and the blocks on it.</summary>
    public sealed class BuiltGrid
    {
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();

        /// <summary>A static grid at <paramref name="at" /> holding <paramref name="blocks" />, each at the cell its Min gives it.</summary>
        public BuiltGrid(Vector3D at, params MyObjectBuilder_CubeBlock[] blocks)
            : this(at, true, blocks)
        {
        }

        /// <summary>A grid at <paramref name="at" />, static where <paramref name="isStatic" /> holds, holding <paramref name="blocks" />, each at the cell its Min gives it.</summary>
        public BuiltGrid(Vector3D at, bool isStatic, params MyObjectBuilder_CubeBlock[] blocks)
            : this(at, Vector3.Forward, Vector3.Up, isStatic, blocks)
        {
        }

        /// <summary>A grid at <paramref name="at" /> turned to <paramref name="forward" /> and <paramref name="up" />, static where <paramref name="isStatic" /> holds, holding <paramref name="blocks" />, each at the cell its Min gives it.</summary>
        public BuiltGrid(Vector3D at, Vector3 forward, Vector3 up, bool isStatic, params MyObjectBuilder_CubeBlock[] blocks)
        {
            var grid = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            grid.GridSizeEnum = MyCubeSize.Large;
            grid.IsStatic = isStatic;
            grid.PersistentFlags = MyPersistentEntityFlags2.InScene;
            grid.PositionAndOrientation = new MyPositionAndOrientation(at, forward, up);
            grid.CubeBlocks.AddRange(blocks);
            Grid = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);
            Grid.GetBlocks(_blocks);
        }

        /// <summary>The grid itself.</summary>
        public IMyCubeGrid Grid { get; private set; }

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

        /// <summary>Every block on the grid that is a <typeparamref name="T" />.</summary>
        public List<T> All<T>() where T : class
        {
            var all = new List<T>();
            foreach (var block in _blocks)
            {
                var found = block.FatBlock as T;
                if (found != null)
                    all.Add(found);
            }

            return all;
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

        /// <summary>A large block of light armor at <paramref name="cell" />.</summary>
        public static MyObjectBuilder_CubeBlock Armor(Vector3I cell)
        {
            var armor = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeBlock>("LargeBlockArmorBlock");
            armor.Min = cell;
            return armor;
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
