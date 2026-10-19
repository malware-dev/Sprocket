using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// The construct the editor was opened on, as Sprocket sees it.
    /// </summary>
    public sealed class GridWorld : IDisposable
    {
        IMyTerminalBlock _anchor;

        readonly BlockKinds _kinds;

        readonly List<GridEntity> _entities = new List<GridEntity>();
        Dictionary<long, GridEntity> _byId = new Dictionary<long, GridEntity>();

        // The entities of the read before, while a rebuild keeps those whose blocks are still there.
        Dictionary<long, GridEntity> _keptById = new Dictionary<long, GridEntity>();

        // The blocks welded on and taken off since the last read, in the order the grid said so.
        readonly List<Change> _changes = new List<Change>();

        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMyCubeGrid> _watched = new List<IMyCubeGrid>();
        IMyGridGroupData _group;
        readonly List<IMySlimBlock> _slim = new List<IMySlimBlock>();
        // Every terminal block on the construct, shared with the big owners or not, each with its id, read once.
        readonly List<Found> _found = new List<Found>();
        readonly Dictionary<long, IMyTerminalBlock> _named = new Dictionary<long, IMyTerminalBlock>();
        readonly List<long> _unheard = new List<long>();
        readonly HashSet<long> _seen = new HashSet<long>();
        readonly Dictionary<long, int> _counted = new Dictionary<long, int>();
        readonly Dictionary<long, long> _ownerOf = new Dictionary<long, long>();
        readonly List<long> _bigOwners = new List<long>();
        readonly List<long> _owners = new List<long>();
        readonly HashSet<long> _unsharedIds = new HashSet<long>();
        readonly List<string> _unshared = new List<string>();
        readonly List<string> _wasUnshared = new List<string>();
        readonly List<string> _returned = new List<string>();

        bool _stale = true;

        // Whether who owns or may use the blocks may have changed since the last read, which the next one looks at again.
        bool _reshared;
        int _revision;

        /// <summary>The construct <paramref name="anchor" /> is on, its blocks of the world's <paramref name="kinds" />, read when it is first asked about.</summary>
        public GridWorld(IMyTerminalBlock anchor, BlockKinds kinds)
        {
            _kinds = kinds;
            Keys = new BlockKeys(this);
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
                Current();

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
                Current();

                return _entities;
            }
        }

        /// <summary>The players who own most of the construct's blocks, counted over every grid of it; empty where nobody owns any.</summary>
        /// <remarks>Several where they tie.</remarks>
        public IReadOnlyList<long> BigOwners
        {
            get
            {
                Current();

                return _bigOwners;
            }
        }

        /// <summary>The names of the construct's blocks none of its big owners may use, which the automation cannot reach.</summary>
        public IReadOnlyList<string> Unshared
        {
            get
            {
                Current();

                return _unshared;
            }
        }

        /// <summary>The names of the blocks the read before this one left out and this one let back in.</summary>
        public IReadOnlyList<string> Returned
        {
            get
            {
                Current();

                return _returned;
            }
        }

        /// <summary>Has the construct read again, as what is shared with whom may have changed: a faction joined, left or at peace.</summary>
        public void Reshare()
        {
            _reshared = true;
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

        /// <summary>The grid the anchor stands on, or null where it has none.</summary>
        public IMyCubeGrid Grid => _anchor?.CubeGrid;

        /// <summary>The keys the program finds the construct's blocks again by, written and matched.</summary>
        public BlockKeys Keys { get; }

        /// <summary>Which sequences nothing on the construct can start, and why.</summary>
        public UnstartableSequences Unstartable { get; } = new UnstartableSequences();

        /// <summary>Whether there is a grid to rename.</summary>
        public bool CanRename => _anchor != null && _anchor.CubeGrid != null;

        /// <summary>Calls the grid the program runs on <paramref name="name" />.</summary>
        public void Rename(string name)
        {
            IMyCubeGrid grid = _anchor?.CubeGrid;
            if (grid != null)
                grid.CustomName = name;
        }

        /// <summary>
        /// Whether toolbar slot <paramref name="at" /> of <paramref name="panel" /> holds something this program did not write: any
        /// of the game's own actions, or another Sprocket's.
        /// </summary>
        /// <remarks>
        /// This Sprocket's action counts as its own whatever key it carries: on the panel itself, as it is written, naming this
        /// Sprocket, as a slot an older build wrote does, or naming a Sprocket that is gone.
        /// </remarks>
        public bool Foreign(IMyTerminalBlock panel, ToolbarSlot at)
        {
            IMyToolbar toolbar = Toolbars.Of(panel, at.Toolbar);
            var slot = Toolbars.Index(toolbar, at);
            MyToolbarItemInfo present;
            if (slot < 0 || !toolbar.TryGetItem(slot, out present))
                return false;

            if (SignalAction.IsOurs(present))
            {
                return _anchor != null && present.BlockEntityId != panel.EntityId && present.BlockEntityId != _anchor.EntityId
                    && Toolbars.Exists(present.BlockEntityId);
            }

            return true;
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

        /// <summary>The block <paramref name="entityId" />, or null where it is not on the construct.</summary>
        public GridEntity FindEntity(long entityId)
        {
            Current();

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

        /// <summary>Brings the block list up to date: read again whole where the construct itself changed, or by the blocks welded on and taken off since.</summary>
        /// <remarks>One flag and one count read when nothing changed.</remarks>
        void Current()
        {
            if (_stale)
            {
                Rebuild();
                return;
            }

            if (_changes.Count > 0)
                Apply();

            // Where Apply read the construct again whole, that has answered this too.
            if (_reshared)
                Reown();
        }

        /// <summary>
        /// Rebuilds the block list from the mechanically connected grids.
        /// </summary>
        /// <remarks>
        /// A block still there keeps the entity it had, so what was worked out against it stands. Asked when a grid joins or
        /// leaves the construct, and when what is shared with whom may have changed; a block welded on or taken off is
        /// <see cref="Apply" />'s.
        /// </remarks>
        void Rebuild()
        {
            _stale = false;
            _reshared = false;
            _changes.Clear();
            _revision++;

            // Swapped rather than copied, so the entities of the last read are there to be kept.
            var kept = _keptById;
            _keptById = _byId;
            _byId = kept;
            _byId.Clear();
            _entities.Clear();

            IMyCubeGrid home = Home();
            if (home == null || MyAPIGateway.GridGroups == null)
            {
                _keptById.Clear();
                return;
            }

            _grids.Clear();
            MyAPIGateway.GridGroups.GetGroup(home, GridLinkTypeEnum.Mechanical, _grids);
            _grids.RemoveAll(Closing);
            Watch(_grids);
            Join(MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Mechanical, home));

            _found.Clear();
            _seen.Clear();
            _counted.Clear();
            _ownerOf.Clear();
            foreach (var grid in _grids)
            {
                _slim.Clear();
                grid.GetBlocks(_slim);

                foreach (var slim in _slim)
                {
                    IMyTerminalBlock block = slim.FatBlock as IMyTerminalBlock;
                    if (block == null)
                        continue;

                    var id = block.EntityId;
                    if (!_seen.Add(id))
                        continue;

                    _found.Add(new Found { Id = id, Block = block });
                    Count(id, block);
                }
            }

            Crown();

            _wasUnshared.Clear();
            _wasUnshared.AddRange(_unshared);
            _unshared.Clear();
            _unsharedIds.Clear();
            _returned.Clear();

            // A block none of the big owners may use is no part of their automation, and reads as gone; the next read after
            // its sharing changes finds it again.
            foreach (var found in _found)
                Admit(found.Id, found.Block);

            _keptById.Clear();
            Hear();
        }

        /// <summary>
        /// Takes in the blocks welded on and taken off since the last read, one by one, where the construct's big owners stay
        /// who they were; reads the construct again whole where they do not, since who may use every block may have changed.
        /// </summary>
        /// <remarks>Asked on the read after a block is welded on or taken off, and costs what those blocks do, whatever the construct's size.</remarks>
        void Apply()
        {
            _owners.Clear();
            _owners.AddRange(_bigOwners);

            for (var i = 0; i < _changes.Count; i++)
            {
                var block = _changes[i].Block;
                var id = block.EntityId;
                if (_changes[i].Added)
                {
                    if (Closing(block) || !_seen.Add(id))
                        continue;

                    _found.Add(new Found { Id = id, Block = block });
                    Count(id, block);
                    Hear(id, block);
                }
                else if (_seen.Remove(id))
                {
                    _found.RemoveAt(_found.FindIndex(found => found.Id == id));
                    Uncount(id);
                    Unhear(id);
                }
            }

            Crown();
            if (!Same(_owners, _bigOwners))
            {
                _stale = true;
                Rebuild();
                return;
            }

            _revision++;
            _wasUnshared.Clear();
            _wasUnshared.AddRange(_unshared);
            _returned.Clear();
            for (var i = 0; i < _changes.Count; i++)
            {
                var block = _changes[i].Block;
                var id = block.EntityId;
                if (_changes[i].Added)
                {
                    if (_seen.Contains(id) && !_byId.ContainsKey(id) && !_unsharedIds.Contains(id))
                        Admit(id, block);
                }
                else
                {
                    Expel(id, block);
                }
            }

            _changes.Clear();
        }

        /// <summary>
        /// Counts every block to its owner again and asks again whether the big owners may use it, taking in and letting go of
        /// the blocks whose answer changed; the construct reads as changed only where any did, or the big owners did.
        /// </summary>
        /// <remarks>
        /// Asked on the read after the grid says a block changed hands, which it says after any block's owner or sharing
        /// changes and after any block comes to work or stops, so in a fight. Reads the blocks already found, and asks no
        /// grid for its blocks; a block whose sharing changed with its owner unchanged is found only by asking each.
        /// </remarks>
        void Reown()
        {
            _reshared = false;
            _owners.Clear();
            _owners.AddRange(_bigOwners);
            foreach (var found in _found)
            {
                long was;
                _ownerOf.TryGetValue(found.Id, out was);
                if (found.Block.OwnerId == was)
                    continue;

                Uncount(found.Id);
                Count(found.Id, found.Block);
            }

            Crown();
            var changed = !Same(_owners, _bigOwners);

            _wasUnshared.Clear();
            _wasUnshared.AddRange(_unshared);
            _returned.Clear();
            foreach (var found in _found)
            {
                var shared = Shared(found.Block);
                if (shared == _byId.ContainsKey(found.Id))
                    continue;

                changed = true;
                if (shared)
                {
                    _unsharedIds.Remove(found.Id);
                    _unshared.Remove(found.Block.CustomName);
                    Admit(found.Id, found.Block);
                    continue;
                }

                Expel(found.Id, found.Block);
                _unshared.Add(found.Block.CustomName);
                _unsharedIds.Add(found.Id);
            }

            if (changed)
                _revision++;
        }

        /// <summary>Takes <paramref name="block" />, <paramref name="id" />, in as one of the construct's, or as a block the big owners may not use.</summary>
        void Admit(long id, IMyTerminalBlock block)
        {
            if (_byId.ContainsKey(id))
                return;

            if (!Shared(block))
            {
                _unshared.Add(block.CustomName);
                _unsharedIds.Add(id);
                return;
            }

            _unsharedIds.Remove(id);
            if (_wasUnshared.Count > 0 && _wasUnshared.Contains(block.CustomName))
                _returned.Add(block.CustomName);

            GridEntity entity;
            if (!_keptById.TryGetValue(id, out entity) || entity.Block != block)
                entity = new GridEntity(block, _kinds.For(block));

            _byId.Add(id, entity);
            _entities.Add(entity);
        }

        /// <summary>Lets <paramref name="block" />, <paramref name="id" />, taken off the construct, go.</summary>
        void Expel(long id, IMyTerminalBlock block)
        {
            GridEntity entity;
            if (_byId.TryGetValue(id, out entity))
            {
                _byId.Remove(id);
                _entities.Remove(entity);
                return;
            }

            if (_unsharedIds.Remove(id))
                _unshared.Remove(block.CustomName);
        }

        /// <summary>Counts <paramref name="block" />, <paramref name="id" />, to its owner, remembering whose it was counted to.</summary>
        void Count(long id, IMyTerminalBlock block)
        {
            var owner = block.OwnerId;
            if (owner == 0)
                return;

            int count;
            _counted.TryGetValue(owner, out count);
            _counted[owner] = count + 1;
            _ownerOf[id] = owner;
        }

        /// <summary>Takes the block <paramref name="id" /> off the count of whoever it was counted to.</summary>
        void Uncount(long id)
        {
            long owner;
            if (!_ownerOf.TryGetValue(id, out owner))
                return;

            _ownerOf.Remove(id);
            var count = _counted[owner] - 1;
            if (count > 0)
                _counted[owner] = count;
            else
                _counted.Remove(owner);
        }

        /// <summary>Whether <paramref name="one" /> and <paramref name="other" /> hold the same players in the same order.</summary>
        static bool Same(List<long> one, List<long> other)
        {
            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (one[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>Listens for the blocks found being renamed, and for no others: a block it already listens to is left as it is.</summary>
        void Hear()
        {
            _unheard.Clear();
            foreach (KeyValuePair<long, IMyTerminalBlock> named in _named)
            {
                if (!_seen.Contains(named.Key))
                    _unheard.Add(named.Key);
            }

            foreach (var id in _unheard)
                Unhear(id);

            foreach (var found in _found)
                Hear(found.Id, found.Block);
        }

        /// <summary>Listens for <paramref name="block" />, <paramref name="id" />, being renamed, in place of any block it listened to under that id.</summary>
        void Hear(long id, IMyTerminalBlock block)
        {
            IMyTerminalBlock heard;
            if (_named.TryGetValue(id, out heard))
            {
                if (heard == block)
                    return;

                heard.CustomNameChanged -= OnRenamed;
            }

            block.CustomNameChanged += OnRenamed;
            _named[id] = block;
        }

        /// <summary>Stops listening for the block <paramref name="id" /> being renamed.</summary>
        void Unhear(long id)
        {
            IMyTerminalBlock heard;
            if (!_named.TryGetValue(id, out heard))
                return;

            heard.CustomNameChanged -= OnRenamed;
            _named.Remove(id);
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

        /// <summary>Whether <paramref name="block" /> is shared with any of the big owners; every block where nobody owns the construct.</summary>
        bool Shared(IMyTerminalBlock block)
        {
            if (_bigOwners.Count == 0)
                return true;

            foreach (var owner in _bigOwners)
            {
                if (Access.Shared(block, owner))
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
            Changed(block, true);
        }

        void OnBlockRemoved(IMySlimBlock block)
        {
            // The construct is found from the anchor's grid, so losing it reads the construct again whole.
            if (block.FatBlock != null && block.FatBlock == _anchor)
            {
                _stale = true;
                if (AnchorRemoved != null)
                    AnchorRemoved();

                return;
            }

            Changed(block, false);
        }

        /// <summary>Keeps <paramref name="slim" />, welded on or taken off as <paramref name="added" /> says, for the next read.</summary>
        /// <remarks>
        /// A block with no terminal, armor and the like, is no part of what the construct reads, and is let go by here: it is
        /// most of what a grid gains and loses. A grid split by losing one says so itself.
        /// </remarks>
        void Changed(IMySlimBlock slim, bool added)
        {
            var block = slim.FatBlock as IMyTerminalBlock;
            if (block != null)
                _changes.Add(new Change { Block = block, Added = added });
        }

        /// <summary>A block welded on or taken off, waiting for the next read.</summary>
        struct Change
        {
            public IMyTerminalBlock Block;
            public bool Added;
        }

        /// <summary>A terminal block on the construct, with its id, read as it was found.</summary>
        struct Found
        {
            public long Id;
            public IMyTerminalBlock Block;
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
            foreach (var heard in _named.Values)
                heard.CustomNameChanged -= OnRenamed;

            _named.Clear();
            _changes.Clear();
            Follow(null);
        }
    }
}
