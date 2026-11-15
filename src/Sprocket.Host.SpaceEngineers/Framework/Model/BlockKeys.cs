using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>The keys a program finds a construct's blocks again by, written and matched: an id, a path across the joints, a name.</summary>
    public sealed class BlockKeys
    {
        readonly GridWorld _world;

        /// <summary>The keys of <paramref name="world" />'s blocks.</summary>
        public BlockKeys(GridWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// How many times a reference has been matched against the blocks here.
        /// </summary>
        /// <remarks>
        /// For measuring: matching walks every key against every block, and must not run per reference per tick. A test
        /// counts it over six hundred idle ticks.
        /// </remarks>
        public int Matched { get; private set; }

        /// <summary>
        /// The keys Sprocket writes for a block, in the order it wants them tried: its id, its path, its name.
        /// </summary>
        /// <remarks>Pasting or blueprinting a grid in the game gives every block a new id and keeps where each block stands on its grid.</remarks>
        public void Identify(GridEntity entity, List<EntityKey> keys)
        {
            keys.Clear();
            keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id)));

            Vector3I[] path = Path(entity);
            if (path != null)
                keys.Add(new EntityKey(path));

            keys.Add(new EntityKey(ValueKind.Text, _world.Values.Text(entity.Name)));
        }

        /// <summary>
        /// Brings the first id, the first path and the first name among <paramref name="keys" /> up to date with
        /// <paramref name="entity" />, the block they surely found, leaving every key where the program put it, and answers
        /// whether any changed.
        /// </summary>
        /// <remarks>
        /// A key that no longer fits goes unnoticed while a stronger one finds the block, until the day it is needed: a copy
        /// has new ids and finds its blocks by their paths, so a path gone stale as the grid changed leaves the copy finding
        /// them by name alone. Walking a path asks one cell of each grid on it; a new path is searched for only where the old
        /// one no longer leads to the block, and a path is kept where none leads there now, as while a joint is detached.
        /// </remarks>
        public bool Renew(GridEntity entity, List<EntityKey> keys)
        {
            var changed = false;
            bool id = false, path = false, name = false;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key.Cells != null)
                {
                    if (path)
                        continue;

                    path = true;
                    var walked = key.Cells.Length == 0 ? null : Walk(key.Cells);
                    if (walked != null && walked.Id == entity.Id)
                        continue;

                    Vector3I[] cells = Path(entity);
                    if (cells == null)
                        continue;

                    keys[i] = new EntityKey(cells);
                    changed = true;
                }
                else if (key.Kind == ValueKind.Integer)
                {
                    if (id)
                        continue;

                    id = true;
                    long had;
                    if (_world.Values.TryInteger(key.Kind, key.Value, out had) && had == entity.Id)
                        continue;

                    keys[i] = new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id));
                    changed = true;
                }
                else if (key.Kind == ValueKind.Text)
                {
                    if (name)
                        continue;

                    name = true;
                    string had;
                    if (_world.Values.TryText(key.Kind, key.Value, out had) && had == entity.Name)
                        continue;

                    keys[i] = new EntityKey(ValueKind.Text, _world.Values.Text(entity.Name));
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>The block the first of <paramref name="keys" /> that finds one finds, with which key it was in <paramref name="matched" />, or null where none does.</summary>
        public GridEntity Match(IList<EntityKey> keys, out int matched)
        {
            Matched++;

            for (var i = 0; i < keys.Count; i++)
            {
                var found = Matching(keys[i]);
                if (found == null)
                    continue;

                matched = i;
                return found;
            }

            matched = -1;
            return null;
        }

        GridEntity Matching(EntityKey key)
        {
            if (key.Cells != null)
                return key.Cells.Length == 0 ? null : Walk(key.Cells);

            var values = _world.Values;
            if (key.Kind == ValueKind.Integer)
            {
                long id;
                values.TryInteger(key.Kind, key.Value, out id);
                return _world.FindEntity(id);
            }

            if (key.Kind != ValueKind.Text)
                return null;

            string name;
            values.TryText(key.Kind, key.Value, out name);
            if (name.Length == 0)
                return null;

            // The first of several: two blocks may share a name.
            foreach (var entity in _world.Entities)
            {
                if (entity.Name == name)
                    return entity;
            }

            return null;
        }

        /// <summary>
        /// How to reach <paramref name="entity" /> from the anchor's own grid: the cell of each
        /// mechanical joint crossed, then the block's own cell. Null where no joint leads there.
        /// </summary>
        /// <remarks>
        /// A cell is a block's <c>Position</c>, which a paste or a blueprint keeps. Searches every joint, so it is asked when a
        /// block is declared and never on a tick.
        /// </remarks>
        Vector3I[] Path(GridEntity entity)
        {
            IMyCubeGrid home = _world.Grid;
            IMyCubeGrid target = entity.Block.CubeGrid;
            if (home == null || target == null)
                return null;

            // Breadth first, so the path is the shortest.
            var from = new Dictionary<IMyCubeGrid, IMyCubeGrid>();
            var through = new Dictionary<IMyCubeGrid, Vector3I>();
            var waiting = new Queue<IMyCubeGrid>();
            var blocks = new List<IMySlimBlock>();
            from[home] = null;
            waiting.Enqueue(home);

            while (waiting.Count > 0 && !from.ContainsKey(target))
            {
                IMyCubeGrid grid = waiting.Dequeue();
                blocks.Clear();
                grid.GetBlocks(blocks);

                foreach (var slim in blocks)
                {
                    IMyCubeGrid next = Across(slim.FatBlock);
                    if (next == null || from.ContainsKey(next))
                        continue;

                    from[next] = grid;
                    through[next] = slim.FatBlock.Position;
                    waiting.Enqueue(next);
                }
            }

            if (!from.ContainsKey(target))
                return null;

            var cells = new List<Vector3I> { entity.Block.Position };
            for (IMyCubeGrid grid = target; from[grid] != null; grid = from[grid])
                cells.Insert(0, through[grid]);

            return cells.ToArray();
        }

        /// <summary>
        /// The block a path of cells leads to from the anchor's own grid, or null where any step of it
        /// finds no joint or no block.
        /// </summary>
        GridEntity Walk(Vector3I[] cells)
        {
            IMyCubeGrid grid = _world.Grid;
            for (var i = 0; grid != null && i < cells.Length - 1; i++)
            {
                IMySlimBlock joint = grid.GetCubeBlock(cells[i]);
                grid = joint == null ? null : Across(joint.FatBlock);
            }

            IMySlimBlock found = grid?.GetCubeBlock(cells[cells.Length - 1]);
            var block = found?.FatBlock as IMyTerminalBlock;
            return block == null ? null : _world.FindEntity(block.EntityId);
        }

        /// <summary>
        /// The grid on the other side of <paramref name="block" />, where it is one end of a mechanical
        /// joint with something attached; otherwise null.
        /// </summary>
        static IMyCubeGrid Across(IMyCubeBlock block)
        {
            var bottom = block as IMyMechanicalConnectionBlock;
            if (bottom != null)
                return bottom.TopGrid;

            var top = block as IMyAttachableTopBlock;
            return top == null || top.Base == null ? null : top.Base.CubeGrid;
        }
    }
}
