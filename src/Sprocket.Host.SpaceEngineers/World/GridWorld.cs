using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Blocks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// The construct the editor was opened on, as Sprocket sees it.
    /// </summary>
    public sealed class GridWorld : IDisposable
    {
        IMyTerminalBlock _anchor;
        readonly ParameterCatalog _catalogue = Catalogue();

        readonly BlockKinds _kinds;

        readonly Capabilities _capabilities = new Capabilities();

        static ParameterCatalog Catalogue()
        {
            // Sprocket's own descriptions go in first, so a mod may override them.
            var catalogue = new ParameterCatalog();
            catalogue.Add(KnownActions.Descriptions(), "");
            ModActions.Read(catalogue);
            return catalogue;
        }

        readonly List<GridEntity> _entities = new List<GridEntity>();
        readonly Dictionary<long, GridEntity> _byId = new Dictionary<long, GridEntity>();

        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMyCubeGrid> _watched = new List<IMyCubeGrid>();
        IMyGridGroupData _group;
        readonly List<IMySlimBlock> _slim = new List<IMySlimBlock>();
        readonly List<IMyTerminalBlock> _found = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _named = new List<IMyTerminalBlock>();
        readonly HashSet<long> _seen = new HashSet<long>();
        readonly Dictionary<long, int> _counted = new Dictionary<long, int>();
        readonly List<long> _bigOwners = new List<long>();
        readonly List<string> _unshared = new List<string>();
        readonly List<string> _wasUnshared = new List<string>();
        readonly List<string> _returned = new List<string>();

        bool _stale = true;
        int _revision;

        /// <summary>The construct <paramref name="anchor" /> is on, read when it is first asked about.</summary>
        public GridWorld(IMyTerminalBlock anchor)
        {
            _kinds = new BlockKinds(_catalogue, _capabilities);
            Follow(anchor);
        }

        /// <summary>Raised when a block on the construct changes hands, or when what is shared with whom may have changed.</summary>
        /// <remarks>From the grid's own event, or from <see cref="Reshare" />; the construct is read again before it is next asked about.</remarks>
        public event Action HandsChanged;

        /// <summary>Raised from the grid's own event when the anchor is removed from the construct, in the middle of the grid changing.</summary>
        /// <remarks>Whatever answers it waits until the grid has finished before it reads the construct again.</remarks>
        public event Action AnchorRemoved;

        /// <summary>Raised from the anchor's own event as the game closes it, in the middle of whatever is closing it.</summary>
        /// <remarks>
        /// A grid deleted whole closes every block on it and reports none of them removed, so this is all that is heard of
        /// it.
        /// </remarks>
        public event Action AnchorClosed;

        /// <summary>Raised from a block's own event when one on the construct is renamed, by anybody.</summary>
        public event Action Renamed;

        /// <summary>
        /// Makes and reads the values this construct deals in, and the lists behind the wide ones.
        /// </summary>
        public Values Values { get; } = new Values();

        /// <summary>
        /// Bumped by <see cref="Rebuild" />, which is the only place the set changes, and rebuilt first where the grid said it changed.
        /// </summary>
        /// <remarks>One flag read when nothing changed.</remarks>
        public int Revision
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _revision;
            }
        }

        /// <summary>
        /// Everything on this construct right now, rebuilt when the grid says it changed rather than
        /// every time it is asked for.
        /// </summary>
        /// <remarks>
        /// The grid the anchor sits on and what rotors, pistons and hinges join to it; never what is docked by a connector,
        /// landing gear or magplate.
        /// </remarks>
        public IReadOnlyList<GridEntity> Entities
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _entities;
            }
        }

        /// <summary>The players who own most of the construct's blocks, counted over every grid of it; empty where nobody owns any.</summary>
        /// <remarks>Several where they tie.</remarks>
        public IReadOnlyList<long> BigOwners
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _bigOwners;
            }
        }

        /// <summary>The names of the construct's blocks none of its big owners may use, which the automation cannot reach.</summary>
        public IReadOnlyList<string> Unshared
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _unshared;
            }
        }

        /// <summary>The names of the blocks the read before this one left out and this one let back in.</summary>
        public IReadOnlyList<string> Returned
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _returned;
            }
        }

        /// <summary>Has the construct read again, as what is shared with whom may have changed: a faction joined, left or at peace.</summary>
        public void Reshare()
        {
            _stale = true;
            if (HandsChanged != null)
                HandsChanged();
        }

        /// <summary>
        /// The construct's name, which in this game is the block's own grid's.
        /// </summary>
        public string Name
        {
            get
            {
                IMyCubeGrid grid = _anchor?.CubeGrid;
                return grid == null ? "" : grid.CustomName;
            }
        }

        /// <summary>Whether there is a grid to rename.</summary>
        public bool CanRename => _anchor != null && _anchor.CubeGrid != null;

        /// <summary>Calls the grid the program runs on <paramref name="name" />.</summary>
        public void Rename(string name)
        {
            IMyCubeGrid grid = _anchor?.CubeGrid;
            if (grid != null)
                grid.CustomName = name;
        }

        readonly HashSet<long> _unstartable = new HashSet<long>();

        readonly Dictionary<long, UnansweredButton> _why = new Dictionary<long, UnansweredButton>();

        /// <summary>
        /// Whether toolbar slot <paramref name="at" /> of <paramref name="panel" /> holds something this program did not write: any
        /// of the game's own actions, or another Sprocket's.
        /// </summary>
        /// <remarks>
        /// This Sprocket's action counts as its own whatever key it carries, and so does one pointed at a Sprocket that is
        /// gone.
        /// </remarks>
        public bool Foreign(IMyTerminalBlock panel, ToolbarSlot at)
        {
            IMyToolbar toolbar = Toolbars.Of(panel, at.Toolbar);
            var slot = Toolbars.Index(toolbar, at);
            MyToolbarItemInfo present;
            if (slot < 0 || !toolbar.TryGetItem(slot, out present))
                return false;

            if (SignalAction.IsOurs(present))
                return _anchor != null && present.BlockEntityId != _anchor.EntityId && Toolbars.Exists(present.BlockEntityId);

            return true;
        }

        /// <summary>How many times what nothing can start has been told again, so what reads it knows to read it again.</summary>
        public int Unstarted { get; private set; }

        /// <summary>Which sequences nothing can start on which one of their system, each as <see cref="Start" /> gives it, and why, where that is known.</summary>
        public void Unstartable(List<long> starts, Dictionary<long, UnansweredButton> why)
        {
            Unstarted++;
            _unstartable.Clear();
            foreach (var start in starts)
                _unstartable.Add(start);

            _why.Clear();
            foreach (var pair in why)
                _why[pair.Key] = pair.Value;
        }

        /// <summary>Why nothing can start <paramref name="sequence" /> on <paramref name="instance" />, or null where that is not known.</summary>
        public UnansweredButton Why(Sequence sequence, SystemInstance instance)
        {
            UnansweredButton why;
            return _why.TryGetValue(Start(sequence.Id, instance), out why) ? why : null;
        }

        /// <summary>Whether anything on the construct can start <paramref name="sequence" /> on <paramref name="instance" />.</summary>
        public bool CanStart(Sequence sequence, SystemInstance instance)
        {
            return _unstartable.Count == 0 || !_unstartable.Contains(Start(sequence.Id, instance));
        }

        /// <summary>One sequence on one of its system, as one number.</summary>
        public static long Start(int sequenceId, SystemInstance instance)
        {
            return ((long)sequenceId << 32) | (uint)(instance?.Number ?? 0);
        }

        bool _asked;

        /// <summary>Asks for the construct to be looked at again on the next tick.</summary>
        public void LookAgain()
        {
            _asked = true;
        }

        /// <summary>Whether something has asked to be looked at again since this was last called.</summary>
        /// <remarks>Read every frame, so it is a flag rather than an event.</remarks>
        public bool TakeLook()
        {
            var asked = _asked;
            _asked = false;
            return asked;
        }

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

            keys.Add(new EntityKey(ValueKind.Text, Values.Text(entity.Name)));
        }

        /// <summary>
        /// Gives the first id among <paramref name="keys" /> the id of <paramref name="entity" />, leaving
        /// every key where the program put it.
        /// </summary>
        /// <remarks>For a block found by its path after its id found nothing.</remarks>
        public void Renumber(GridEntity entity, List<EntityKey> keys)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                if (keys[i].Cells != null || keys[i].Kind != ValueKind.Integer)
                    continue;

                keys[i] = new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id));
                return;
            }
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
            IMyCubeGrid home = _anchor?.CubeGrid;
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
            IMyCubeGrid grid = _anchor?.CubeGrid;
            for (var i = 0; grid != null && i < cells.Length - 1; i++)
            {
                IMySlimBlock joint = grid.GetCubeBlock(cells[i]);
                grid = joint == null ? null : Across(joint.FatBlock);
            }

            IMySlimBlock found = grid?.GetCubeBlock(cells[cells.Length - 1]);
            var block = found?.FatBlock as IMyTerminalBlock;
            return block == null ? null : FindEntity(block.EntityId);
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

        /// <summary>
        /// How many times a reference has been matched against the blocks here.
        /// </summary>
        /// <remarks>
        /// For measuring: matching walks every key against every block, and must not run per reference per tick. A test
        /// counts it over six hundred idle ticks.
        /// </remarks>
        public int Matched { get; private set; }

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

            if (key.Kind == ValueKind.Integer)
            {
                long id;
                Values.TryInteger(key.Kind, key.Value, out id);
                return FindEntity(id);
            }

            if (key.Kind != ValueKind.Text)
                return null;

            string name;
            Values.TryText(key.Kind, key.Value, out name);
            if (name.Length == 0)
                return null;

            // The first of several: two blocks may share a name.
            foreach (var entity in _entities)
            {
                if (entity.Name == name)
                    return entity;
            }

            return null;
        }

        /// <summary>The block <paramref name="entityId" />, or null where it is not on the construct.</summary>
        public GridEntity FindEntity(long entityId)
        {
            if (_stale)
                Rebuild();

            GridEntity entity;
            return _byId.TryGetValue(entityId, out entity) ? entity : null;
        }

        /// <summary>Tells a block to do something.</summary>
        public void Invoke(GridEntity entity, string actionId)
        {
            Invoke(entity, actionId, null);
        }

        /// <summary>Tells a block to do something, with the values <paramref name="arguments" /> the action asked for.</summary>
        public void Invoke(GridEntity entity, string actionId, IList<Literal> arguments)
        {
            Invoke(entity, actionId, arguments, ReachKind.Native);
        }

        /// <summary>Tells a block to do something, with the values <paramref name="arguments" /> the action asked for, <paramref name="form" /> saying which of the three ways the step reaches the block.</summary>
        public void Invoke(GridEntity entity, string actionId, IList<Literal> arguments, ReachKind form)
        {
            if (entity != null)
                entity.Invoke(actionId, arguments, form);
        }

        /// <summary>
        /// Rebuilds the block list from the mechanically connected grids.
        /// </summary>
        void Rebuild()
        {
            _stale = false;
            _revision++;
            _entities.Clear();
            _byId.Clear();
            IMyCubeGrid home = Home();
            if (home == null || MyAPIGateway.GridGroups == null)
                return;

            _grids.Clear();
            MyAPIGateway.GridGroups.GetGroup(home, GridLinkTypeEnum.Mechanical, _grids);
            _grids.RemoveAll(Closing);
            Watch(_grids);
            Join(MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Mechanical, home));

            _found.Clear();
            _seen.Clear();
            _counted.Clear();
            foreach (var grid in _grids)
            {
                _slim.Clear();
                grid.GetBlocks(_slim);

                foreach (var slim in _slim)
                {
                    IMyTerminalBlock block = slim.FatBlock as IMyTerminalBlock;
                    if (block == null || !_seen.Add(block.EntityId))
                        continue;

                    _found.Add(block);
                    var owner = block.OwnerId;
                    if (owner == 0)
                        continue;

                    int count;
                    _counted.TryGetValue(owner, out count);
                    _counted[owner] = count + 1;
                }
            }

            Crown();

            _wasUnshared.Clear();
            _wasUnshared.AddRange(_unshared);
            _unshared.Clear();
            _returned.Clear();

            // A block none of the big owners may use is no part of their automation, and reads as gone; the next read after
            // its sharing changes finds it again.
            foreach (var block in _found)
            {
                if (_byId.ContainsKey(block.EntityId))
                    continue;

                if (block != _anchor && !Shared(block))
                {
                    _unshared.Add(block.CustomName);
                    continue;
                }

                if (_wasUnshared.Contains(block.CustomName))
                    _returned.Add(block.CustomName);

                GridEntity entity = new GridEntity(block, _kinds.For(block));
                _byId.Add(block.EntityId, entity);
                _entities.Add(entity);
            }

            Hear(_found);
        }

        /// <summary>Listens for <paramref name="blocks" /> being renamed, and for no others.</summary>
        void Hear(List<IMyTerminalBlock> blocks)
        {
            foreach (var block in _named)
                block.CustomNameChanged -= OnRenamed;

            _named.Clear();
            foreach (var block in blocks)
            {
                block.CustomNameChanged += OnRenamed;
                _named.Add(block);
            }
        }

        void OnRenamed(IMyTerminalBlock block)
        {
            if (Renamed != null)
                Renamed();
        }

        /// <summary>Makes the players owning the most blocks counted the construct's big owners.</summary>
        void Crown()
        {
            _bigOwners.Clear();
            var most = 0;
            foreach (KeyValuePair<long, int> owner in _counted)
            {
                if (owner.Value < most)
                    continue;

                if (owner.Value > most)
                {
                    most = owner.Value;
                    _bigOwners.Clear();
                }

                _bigOwners.Add(owner.Key);
            }

            _bigOwners.Sort();
        }

        /// <summary>Whether any of the big owners may use <paramref name="block" />, as the game's own access test answers it; every block where nobody owns the construct.</summary>
        bool Shared(IMyTerminalBlock block)
        {
            if (_bigOwners.Count == 0)
                return true;

            foreach (var owner in _bigOwners)
            {
                if (block.HasPlayerAccess(owner))
                    return true;
            }

            return false;
        }

        /// <summary>The grid the construct is found from: the anchor's, or where the anchor is gone, one the construct had that is still there.</summary>
        IMyCubeGrid Home()
        {
            if (_anchor != null && !Closing(_anchor) && _anchor.CubeGrid != null && !Closing(_anchor.CubeGrid))
                return _anchor.CubeGrid;

            foreach (var grid in _watched)
            {
                if (!Closing(grid))
                    return grid;
            }

            return null;
        }

        /// <summary>Whether the game has closed <paramref name="entity" /> or is about to, either of which leaves it no part of the construct.</summary>
        /// <remarks>The game marks a grid deleted whole first and closes it later in the frame, leaving it in its group until then.</remarks>
        static bool Closing(IMyEntity entity)
        {
            return entity.MarkedForClose || entity.Closed;
        }

        /// <summary>Anchors the construct on <paramref name="anchor" />, the programmable block that holds the automation from here on.</summary>
        public void Anchor(IMyTerminalBlock anchor)
        {
            Follow(anchor);
            _stale = true;
        }

        /// <summary>Makes <paramref name="anchor" /> the anchor, listening for it closing in place of the one before it.</summary>
        void Follow(IMyTerminalBlock anchor)
        {
            if (_anchor != null)
                _anchor.OnMarkForClose -= OnAnchorClosed;

            _anchor = anchor;
            if (_anchor != null)
                _anchor.OnMarkForClose += OnAnchorClosed;
        }

        /// <summary>Listens to whatever grids are currently part of this construct, and nothing else.</summary>
        void Watch(List<IMyCubeGrid> grids)
        {
            foreach (var grid in _watched)
            {
                grid.OnBlockAdded -= OnBlocksChanged;
                grid.OnBlockRemoved -= OnBlockRemoved;
                grid.OnBlockOwnershipChanged -= OnOwnership;
                grid.OnGridSplit -= OnGridsChanged;
                grid.OnGridMerge -= OnGridsChanged;
            }

            _watched.Clear();

            foreach (var grid in grids)
            {
                grid.OnBlockAdded += OnBlocksChanged;
                grid.OnBlockRemoved += OnBlockRemoved;
                grid.OnBlockOwnershipChanged += OnOwnership;
                grid.OnGridSplit += OnGridsChanged;
                grid.OnGridMerge += OnGridsChanged;
                _watched.Add(grid);
            }
        }

        /// <summary>Listens to the mechanical group the construct's own grid is in, which says when a grid joins or leaves it.</summary>
        /// <remarks>
        /// The game moves a grid between groups as a rotor, piston or hinge attaches or detaches and as grids split or merge,
        /// adding or removing no block. Called on every rebuild.
        /// </remarks>
        void Join(IMyGridGroupData group)
        {
            if (group == _group)
                return;

            if (_group != null)
            {
                _group.OnGridAdded -= OnGroupChanged;
                _group.OnGridRemoved -= OnGroupChanged;
                _group.OnReleased -= OnGroupReleased;
            }

            _group = group;
            if (_group == null)
                return;

            _group.OnGridAdded += OnGroupChanged;
            _group.OnGridRemoved += OnGroupChanged;
            _group.OnReleased += OnGroupReleased;
        }

        void OnGroupChanged(IMyGridGroupData group, IMyCubeGrid grid, IMyGridGroupData other)
        {
            _stale = true;
        }

        void OnGroupReleased(IMyGridGroupData group)
        {
            _stale = true;
        }

        void OnGridsChanged(IMyCubeGrid grid, IMyCubeGrid other)
        {
            _stale = true;
        }

        void OnOwnership(IMyCubeGrid grid)
        {
            Reshare();
        }

        void OnBlocksChanged(IMySlimBlock block)
        {
            _stale = true;
        }

        void OnBlockRemoved(IMySlimBlock block)
        {
            _stale = true;
            if (block.FatBlock != null && block.FatBlock == _anchor && AnchorRemoved != null)
                AnchorRemoved();
        }

        void OnAnchorClosed(IMyEntity anchor)
        {
            _stale = true;
            if (AnchorClosed != null)
                AnchorClosed();
        }

        /// <summary>Stops listening to the grids this was watching.</summary>
        public void Dispose()
        {
            Watch(new List<IMyCubeGrid>());
            Join(null);
            Hear(new List<IMyTerminalBlock>());
            Follow(null);
        }
    }
}
