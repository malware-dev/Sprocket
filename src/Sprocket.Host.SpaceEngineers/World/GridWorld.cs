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
    /// <remarks>
    /// Every block on it is a <see cref="GridEntity" /> of a kind described once per definition, by the
    /// capabilities that claim it and by what the block itself declares - see <see cref="BlockKinds" />.
    /// </remarks>
    public sealed class GridWorld : IDisposable
    {
        IMyTerminalBlock _anchor;
        readonly Catalogue _catalogue = Catalogue();

        readonly BlockKinds _kinds;

        readonly Capabilities _capabilities = new Capabilities();

        static Catalogue Catalogue()
        {
            // Sprocket's own descriptions go in first, so a mod may override them, which is the rule the mods
            // themselves follow.
            var catalogue = new Catalogue();
            catalogue.Add(Known.Descriptions(), "");
            Mods.Read(catalogue);
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
        /// <remarks>Whatever answers it waits until the grid has finished, before it reads the construct again - see <see cref="SprocketUnit" />.</remarks>
        public event Action AnchorRemoved;

        /// <summary>Raised from the anchor's own event as the game closes it, in the middle of whatever is closing it.</summary>
        /// <remarks>
        /// A block removed by itself is closed too, after <see cref="AnchorRemoved" />. A grid deleted whole closes every block on
        /// it and reports none of them removed, so this is all that is heard of it. Answered as that is.
        /// </remarks>
        public event Action AnchorClosed;

        /// <summary>Raised from a block's own event when one on the construct is renamed, by anybody.</summary>
        /// <remarks>Everything that names a block on screen reads its current name, so what shows one says it again.</remarks>
        public event Action Renamed;

        /// <summary>
        /// Makes and reads the values this construct deals in, and the lists behind the wide ones.
        /// </summary>
        /// <remarks>
        /// One store, because a comparison meets a reading its entities produced and an operand declared
        /// on one of their kinds, and a slot only means anything against the lists it was taken from.
        /// Anything that fits in eight bytes needs none of this - see <see cref="EntityValue.Of(bool)" /> -
        /// so this is only reached for to carry something wider, which today means text.
        /// </remarks>
        public Values Values { get; } = new Values();

        /// <summary>
        /// Bumped by <see cref="Rebuild" />, which is the only place the set changes, and rebuilt first where the grid said it changed.
        /// </summary>
        /// <remarks>
        /// Read to decide whether anything that resolves names against the set must do it again, so a change the grid
        /// reported has to count here before the set is next read, or a block ground off goes on resolving as present.
        /// One flag read when nothing changed.
        /// <para>
        /// A rotor or piston head detaching, and a grid splitting or merging, take grids out of this construct or bring
        /// them in without adding or removing a block; the construct's mechanical group says so, and so does each grid
        /// that splits or merges, and both mark it stale.
        /// </para>
        /// </remarks>
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
        /// Blocks get welded on and shot off while a program runs, so this cannot be read once and
        /// kept. Walking every grid in the group on every access would be honest and far too expensive
        /// on a server, so the grid's own <c>OnBlockAdded</c> and <c>OnBlockRemoved</c> mark it stale
        /// instead. Same answer, paid for once per change rather than once per look.
        /// <para>
        /// <b>This construct only</b>: the grid the Sprocket unit sits on plus anything mechanically
        /// connected to it by rotors, pistons and hinges. Connectors, landing gear and magplates are
        /// docking rather than construction, so whatever is on the other side of one is somebody else's
        /// machine and never appears here. That rule is what stops a rule's meaning from changing
        /// underneath somebody when they dock, which in this game is routine rather than an edge case.
        /// </para>
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
        /// <remarks>
        /// The game's big owners, taken over the whole construct rather than one grid, so a programmable block placed on a wing
        /// makes nobody the owner of the construct's automation. Several where they tie.
        /// </remarks>
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
        /// <remarks>
        /// The grid the Sprocket sits on rather than any of the ones welded to it. A construct is several
        /// grids joined by rotors and pistons and each of them has a name of its own, so there is no one
        /// answer - but there is an obvious one, and it is the thing somebody opened the terminal on.
        /// <para>
        /// Never empty: Space Engineers names every grid, and a name the game made up reads no differently
        /// from one nobody has got round to changing. There is no way to tell those apart and no reason to.
        /// </para>
        /// </remarks>
        public string Name
        {
            get
            {
                IMyCubeGrid grid = _anchor == null ? null : _anchor.CubeGrid;
                return grid == null ? "" : grid.CustomName;
            }
        }

        /// <summary>Whether there is a grid to rename.</summary>
        public bool CanRename => _anchor != null && _anchor.CubeGrid != null;

        /// <summary>Calls the grid the program runs on <paramref name="name" />.</summary>
        public void Rename(string name)
        {
            IMyCubeGrid grid = _anchor == null ? null : _anchor.CubeGrid;
            if (grid != null)
                grid.CustomName = name;
        }

        readonly HashSet<long> _unstartable = new HashSet<long>();

        readonly Dictionary<long, Unanswered> _why = new Dictionary<long, Unanswered>();

        /// <summary>
        /// Whether toolbar slot <paramref name="at" /> of <paramref name="panel" /> holds something this program did not write: any
        /// of the game's own actions, or another Sprocket's.
        /// </summary>
        /// <remarks>
        /// Read from the toolbar as it is now. This Sprocket's action counts as its own whatever key it carries, and so does one
        /// pointed at a Sprocket that is gone, since nobody's program answers it.
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
        public void Unstartable(List<long> starts, Dictionary<long, Unanswered> why)
        {
            Unstarted++;
            _unstartable.Clear();
            for (var i = 0; i < starts.Count; i++)
                _unstartable.Add(starts[i]);

            _why.Clear();
            foreach (var pair in why)
                _why[pair.Key] = pair.Value;
        }

        /// <summary>Why nothing can start <paramref name="sequence" /> on <paramref name="instance" />, or null where that is not known.</summary>
        public Unanswered Why(Sequence sequence, SystemInstance instance)
        {
            Unanswered why;
            return _why.TryGetValue(Start(sequence.Id, instance), out why) ? why : null;
        }

        /// <summary>Whether anything on the construct can start <paramref name="sequence" /> on <paramref name="instance" />.</summary>
        public bool CanStart(Sequence sequence, SystemInstance instance)
        {
            return _unstartable.Count == 0 || !_unstartable.Contains(Start(sequence.Id, instance));
        }

        /// <summary>One sequence on one of its system, as one number.</summary>
        /// <remarks>
        /// Each one of a system answers its triggers with its own blocks, so whether a sequence can be
        /// started is a question about the pair: a button filled in the front airlock starts the front
        /// airlock whether or not the port airlock has had its button given yet.
        /// </remarks>
        public static long Start(int sequenceId, SystemInstance instance)
        {
            return ((long)sequenceId << 32) | (uint)(instance == null ? 0 : instance.Number);
        }

        bool _asked;

        /// <summary>Asks for the construct to be looked at again on the next tick.</summary>
        public void LookAgain()
        {
            _asked = true;
        }

        /// <summary>Whether something has asked to be looked at again since this was last called.</summary>
        /// <remarks>
        /// A flag the unit clears rather than an event, because the unit is already ticking and one
        /// boolean read a frame is cheaper than anything that has to be subscribed to and unsubscribed.
        /// </remarks>
        public bool TakeLook()
        {
            var asked = _asked;
            _asked = false;
            return asked;
        }

        /// <summary>
        /// The keys Sprocket writes for a block, in the order it wants them tried: its id, its path, its name.
        /// </summary>
        /// <remarks>
        /// The path is the one that survives a copy. Pasting or blueprinting a grid gives every block a
        /// new id and keeps where each block stands on its grid, so a copy's blocks are found by the
        /// path while the id finds nothing - see <see cref="Path" />. A name is last, because two blocks
        /// can share one.
        /// </remarks>
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
        /// <remarks>
        /// For a block found by its path after its id found nothing, which is a copy's block: from the
        /// next save on, its id finds it first, as it does an original's.
        /// </remarks>
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
        /// A cell is a block's <c>Position</c>, its anchor on its own grid, which a paste or a blueprint
        /// keeps. A joint crossed from its base leads to its top's grid, and from its top to its base's,
        /// so a programmable block on a rotor head reaches the grid the rotor stands on as easily as
        /// the other way round. Worked out by searching the joints, which happens when a block is
        /// declared and never on a tick.
        /// </remarks>
        Vector3I[] Path(GridEntity entity)
        {
            IMyCubeGrid home = _anchor == null ? null : _anchor.CubeGrid;
            IMyCubeGrid target = entity.Block.CubeGrid;
            if (home == null || target == null)
                return null;

            // Breadth first, so the path is the shortest: each grid reached remembers the grid it was
            // reached from and the cell of the joint crossed to get there.
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

                for (var i = 0; i < blocks.Count; i++)
                {
                    IMyCubeGrid next = Across(blocks[i].FatBlock);
                    if (next == null || from.ContainsKey(next))
                        continue;

                    from[next] = grid;
                    through[next] = blocks[i].FatBlock.Position;
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
            IMyCubeGrid grid = _anchor == null ? null : _anchor.CubeGrid;
            for (var i = 0; grid != null && i < cells.Length - 1; i++)
            {
                IMySlimBlock joint = grid.GetCubeBlock(cells[i]);
                grid = joint == null ? null : Across(joint.FatBlock);
            }

            IMySlimBlock found = grid == null ? null : grid.GetCubeBlock(cells[cells.Length - 1]);
            var block = found == null ? null : found.FatBlock as IMyTerminalBlock;
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
        /// <b>For measuring, and it is not idle curiosity.</b> Matching walks every key against every
        /// block, and doing it per reference per tick inside the game's update loop is the failure
        /// this design is shaped to avoid. The wrong version works perfectly and is invisible in every
        /// screenshot, so the only honest proof is a count - which a test takes before and after six
        /// hundred idle ticks.
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

            // Deliberately the first of several. Two blocks may share a name, and picking one is
            // still better than losing the reference - it is a proposal either way, because a name
            // is never the strongest key.
            for (var i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Name == name)
                    return _entities[i];
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

        /// <summary>The same, with the values the action asked for.</summary>
        public void Invoke(GridEntity entity, string actionId, IList<Literal> arguments)
        {
            Invoke(entity, actionId, arguments, Form.Native);
        }

        /// <summary>The same, told which of the three ways the step reaches the block.</summary>
        public void Invoke(GridEntity entity, string actionId, IList<Literal> arguments, Form form)
        {
            if (entity != null)
                entity.Invoke(actionId, arguments, form);
        }

        /// <summary>
        /// Rebuilds the block list from the mechanically connected grids.
        /// </summary>
        /// <remarks>
        /// <c>GridLinkTypeEnum.Mechanical</c> and not <c>Logical</c>, because that is exactly the rule
        /// <see cref="Entities" /> states: rotors, pistons and hinges are this machine, and a connector
        /// is somebody else's parked next to it. The game supplies the predicate, and only the right one
        /// has to be picked.
        /// </remarks>
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
            for (int i = 0; i < _grids.Count; i++)
            {
                _slim.Clear();
                _grids[i].GetBlocks(_slim);

                for (int b = 0; b < _slim.Count; b++)
                {
                    IMyTerminalBlock block = _slim[b].FatBlock as IMyTerminalBlock;
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
            for (int i = 0; i < _found.Count; i++)
            {
                IMyTerminalBlock block = _found[i];
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
            for (int i = 0; i < _named.Count; i++)
                _named[i].CustomNameChanged -= OnRenamed;

            _named.Clear();
            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].CustomNameChanged += OnRenamed;
                _named.Add(blocks[i]);
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

            for (var i = 0; i < _bigOwners.Count; i++)
            {
                if (block.HasPlayerAccess(_bigOwners[i]))
                    return true;
            }

            return false;
        }

        /// <summary>The grid the construct is found from: the anchor's, or where the anchor is gone, one the construct had that is still there.</summary>
        /// <remarks>
        /// The anchor goes when the programmable block holding the automation is destroyed, and the construct is then found
        /// again from what is left of it, so another programmable block can take the automation over. See
        /// <see cref="SprocketUnit" />.
        /// </remarks>
        IMyCubeGrid Home()
        {
            if (_anchor != null && !Closing(_anchor) && _anchor.CubeGrid != null && !Closing(_anchor.CubeGrid))
                return _anchor.CubeGrid;

            for (var i = 0; i < _watched.Count; i++)
            {
                if (!Closing(_watched[i]))
                    return _watched[i];
            }

            return null;
        }

        /// <summary>Whether the game has closed <paramref name="entity" /> or is about to, either of which leaves it no part of the construct.</summary>
        /// <remarks>
        /// A grid deleted whole is marked first and closed later in the frame, and stays in its group until then, so a construct
        /// read in between would still count it.
        /// </remarks>
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
            for (int i = 0; i < _watched.Count; i++)
            {
                _watched[i].OnBlockAdded -= OnBlocksChanged;
                _watched[i].OnBlockRemoved -= OnBlockRemoved;
                _watched[i].OnBlockOwnershipChanged -= OnOwnership;
                _watched[i].OnGridSplit -= OnGridsChanged;
                _watched[i].OnGridMerge -= OnGridsChanged;
            }

            _watched.Clear();

            for (int i = 0; i < grids.Count; i++)
            {
                grids[i].OnBlockAdded += OnBlocksChanged;
                grids[i].OnBlockRemoved += OnBlockRemoved;
                grids[i].OnBlockOwnershipChanged += OnOwnership;
                grids[i].OnGridSplit += OnGridsChanged;
                grids[i].OnGridMerge += OnGridsChanged;
                _watched.Add(grids[i]);
            }
        }

        /// <summary>Listens to the mechanical group the construct's own grid is in, which says when a grid joins or leaves it.</summary>
        /// <remarks>
        /// The game moves a grid from one group to another whenever a rotor, piston or hinge attaches or detaches, and when
        /// grids split or merge, and each group it leaves or joins says so; none of those adds or removes a block. Followed
        /// again on every rebuild, since the construct's own grid may be the one that moved.
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
