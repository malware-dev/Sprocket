using System;
using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Reactor.Host.SpaceEngineers
{
    /// <summary>
    /// The construct the editor was opened on, as Reactor sees it.
    /// <para>
    /// <b>This is the universal floor and nothing more.</b> Every block gets the same small vocabulary -
    /// whether it is switched on, whether it is working, and the two actions that change the first. That is
    /// deliberate rather than unfinished: <c>docs/Catalogue.md</c> argues it is the one thing every block on
    /// the surface can answer, modded blocks included, and therefore the thing to build before anything that
    /// knows what a door is.
    /// </para>
    /// <para>
    /// What is not here yet is the base layer proper, which reads a block's own terminal properties and
    /// actions, and the extensions above it.
    /// </para>
    /// </summary>
    public sealed class GridWorld : IWorld, IDisposable
    {
        /// <summary>
        /// What Space Engineers calls these. Core never writes a player's noun into a sentence itself, so
        /// this is the only place the word lives.
        /// </summary>
        static readonly EntityNoun Blocks = new EntityNoun("block", "blocks", "Blocks");

        readonly IMyTerminalBlock _anchor;
        readonly BlockKinds _kinds = new BlockKinds();

        readonly List<IEntity> _entities = new List<IEntity>();
        readonly Dictionary<long, GridEntity> _byId = new Dictionary<long, GridEntity>();

        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMyCubeGrid> _watched = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> _slim = new List<IMySlimBlock>();
        readonly List<IBlockListener> _listeners = new List<IBlockListener>();

        bool _stale = true;

        public GridWorld(IMyTerminalBlock anchor)
        {
            _anchor = anchor;
        }

        /// <summary>
        /// Everything on this construct, rebuilt when the grid says it changed rather than every time it is
        /// asked for.
        /// </summary>
        /// <remarks>
        /// <see cref="IWorld.Entities" /> asks for this to be re-read rather than cached, because blocks get
        /// welded on and shot off while a program runs. Walking every grid in the group on every access
        /// would do that honestly and be far too expensive on a server, so the grid's own
        /// <c>OnBlockAdded</c> and <c>OnBlockRemoved</c> mark it stale instead. Same answer, paid for once
        /// per change rather than once per look.
        /// </remarks>
        public IReadOnlyList<IEntity> Entities
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _entities;
            }
        }

        public EntityNoun Noun
        {
            get { return Blocks; }
        }

        public IEntity FindEntity(long entityId)
        {
            if (_stale)
                Rebuild();

            GridEntity entity;
            return _byId.TryGetValue(entityId, out entity) ? entity : null;
        }

        public void Invoke(IEntity entity, string actionId)
        {
            GridEntity target = entity as GridEntity;
            if (target == null)
                return;

            target.Invoke(actionId);
        }

        /// <summary>
        /// Rebuilds the block list from the mechanically connected grids.
        /// </summary>
        /// <remarks>
        /// <c>GridLinkTypeEnum.Mechanical</c> and not <c>Logical</c>, because that is exactly the rule
        /// <see cref="IWorld" /> states: rotors, pistons and hinges are this machine, and a connector is
        /// somebody else's parked next to it. The game supplies the predicate; we only have to pick the
        /// right one.
        /// </remarks>
        void Rebuild()
        {
            _stale = false;
            _entities.Clear();
            _byId.Clear();
            DropListeners();

            if (_anchor == null || _anchor.CubeGrid == null || MyAPIGateway.GridGroups == null)
                return;

            _grids.Clear();
            MyAPIGateway.GridGroups.GetGroup(_anchor.CubeGrid, GridLinkTypeEnum.Mechanical, _grids);
            Watch(_grids);

            for (int i = 0; i < _grids.Count; i++)
            {
                _slim.Clear();
                _grids[i].GetBlocks(_slim, null);

                for (int b = 0; b < _slim.Count; b++)
                {
                    IMyTerminalBlock block = _slim[b].FatBlock as IMyTerminalBlock;
                    if (block == null || _byId.ContainsKey(block.EntityId))
                        continue;

                    IBlockExtension extension = BlockExtensions.For(block);
                    IBlockListener listener = extension == null ? null : extension.Listen(block);
                    if (listener != null)
                        _listeners.Add(listener);

                    GridEntity entity = new GridEntity(block, _kinds.For(block), extension, listener);
                    _byId.Add(block.EntityId, entity);
                    _entities.Add(entity);
                }
            }
        }

        /// <summary>Listens to whatever grids are currently part of this construct, and nothing else.</summary>
        void Watch(List<IMyCubeGrid> grids)
        {
            for (int i = 0; i < _watched.Count; i++)
            {
                _watched[i].OnBlockAdded -= OnBlocksChanged;
                _watched[i].OnBlockRemoved -= OnBlocksChanged;
            }

            _watched.Clear();

            for (int i = 0; i < grids.Count; i++)
            {
                grids[i].OnBlockAdded += OnBlocksChanged;
                grids[i].OnBlockRemoved += OnBlocksChanged;
                _watched.Add(grids[i]);
            }
        }

        void OnBlocksChanged(IMySlimBlock block)
        {
            _stale = true;
        }

        /// <summary>
        /// Marks the list for rebuilding. For the moments the grid cannot tell us about, above all a piston
        /// or rotor attaching, which changes which grids are in the group without adding a block to any of
        /// them.
        /// </summary>
        public void Invalidate()
        {
            _stale = true;
        }

        /// <summary>
        /// Forgets every moment a block reported, now that a tick has had its chance to see one.
        /// </summary>
        /// <remarks>
        /// Called after the tick rather than before it. A button press is a moment the game reports and
        /// then forgets, so it is held exactly long enough for core to sample it once and no longer.
        /// </remarks>
        public void ClearMoments()
        {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].Clear();
        }

        void DropListeners()
        {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].Dispose();

            _listeners.Clear();
        }

        public void Dispose()
        {
            DropListeners();
            Watch(new List<IMyCubeGrid>());
        }
    }

    /// <summary>One block, answering the small set of questions every block can answer.</summary>
    sealed class GridEntity : IEntity
    {
        internal const string OnProperty = "on";
        internal const string WorkingProperty = "working";

        readonly IMyTerminalBlock _block;
        readonly EntityKind _kind;
        readonly IBlockExtension _extension;
        readonly IBlockListener _listener;

        public GridEntity(IMyTerminalBlock block, EntityKind kind, IBlockExtension extension,
            IBlockListener listener)
        {
            _block = block;
            _kind = kind;
            _extension = extension;
            _listener = listener;
        }

        public long Id
        {
            get { return _block.EntityId; }
        }

        public string Name
        {
            get { return _block.CustomName; }
        }

        public EntityKind Kind
        {
            get { return _kind; }
        }

        /// <summary>
        /// Live state in the player's own words.
        /// </summary>
        /// <remarks>
        /// Four answers rather than two, because "off" and "no power" are different problems and telling
        /// somebody the wrong one is worse than telling them nothing. Damage comes first because a damaged
        /// block is not working for a reason no switch will fix.
        /// </remarks>
        public string Status
        {
            get
            {
                // What this block is doing, when something knows. "closing" beats "on" on a list of forty.
                if (_extension != null)
                {
                    string told = _extension.StatusOf(_block);
                    if (!string.IsNullOrEmpty(told))
                        return told;
                }

                if (!_block.IsFunctional)
                    return "damaged";

                IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
                if (functional != null && !functional.Enabled)
                    return "off";

                return _block.IsWorking ? "on" : "no power";
            }
        }

        /// <summary>
        /// Whether this block is doing something right now.
        /// </summary>
        /// <remarks>
        /// Not whether it works. A row in the ship list lights up on this, and answering it with "powered
        /// and switched on" lit every row on the ship, which made the light mean nothing at all. Only
        /// something that knows the block can say it is in motion, so a block nobody has written an
        /// extension for is never busy.
        /// </remarks>
        public bool IsActive
        {
            get { return _extension != null && _extension.IsBusy(_block); }
        }

        public EntityValue GetValue(string propertyId)
        {
            EntityValue told;

            // What the block told us, then what it can be asked, then the floor every block stands on.
            if (_listener != null && _listener.TryRead(propertyId, out told))
                return told;

            if (_extension != null && _extension.TryRead(_block, propertyId, out told))
                return told;

            if (propertyId == OnProperty)
            {
                IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
                return EntityValue.Of(functional != null && functional.Enabled);
            }

            if (propertyId == WorkingProperty)
                return EntityValue.Of(_block.IsWorking);

            // An unknown name gives back a default rather than throwing: a saved sequence can outlive the
            // vocabulary it was written against, and that is a thing to surface calmly.
            return EntityValue.Of(false);
        }

        public void Invoke(string actionId)
        {
            if (_extension != null && _extension.TryInvoke(_block, actionId))
                return;

            IMyFunctionalBlock functional = _block as IMyFunctionalBlock;
            if (functional == null)
                return;

            if (actionId == BlockKinds.SwitchOn)
                functional.Enabled = true;
            else if (actionId == BlockKinds.SwitchOff)
                functional.Enabled = false;
        }
    }

    /// <summary>
    /// One <see cref="EntityKind" /> per block definition, built the first time one of that block is seen.
    /// </summary>
    /// <remarks>
    /// Per definition rather than one kind for everything, because the ship list groups by kind and a list
    /// where every row says "Block" is not a list anybody can use. The vocabulary is identical across all of
    /// them for now; what differs is the name the game gives the type.
    /// </remarks>
    sealed class BlockKinds
    {
        internal const string SwitchOn = "on";
        internal const string SwitchOff = "off";

        readonly Dictionary<string, EntityKind> _kinds = new Dictionary<string, EntityKind>();

        public EntityKind For(IMyTerminalBlock block)
        {
            string id = block.BlockDefinition.TypeIdString + "/" + block.BlockDefinition.SubtypeName;

            EntityKind kind;
            if (_kinds.TryGetValue(id, out kind))
                return kind;

            string name = string.IsNullOrEmpty(block.DefinitionDisplayNameText)
                ? block.BlockDefinition.SubtypeName
                : block.DefinitionDisplayNameText;

            // The block's own definition id doubles as its icon: the screen-space sprite API resolves
            // one straight to that block's picture, so nothing has to be declared, extracted or shipped.
            // It is the same string as the kind id, which is not a coincidence - both are "which block
            // type is this".
            kind = new EntityKind(id, name).WithIcon(id);

            // What this block is, before what every block is, so a door offers "open" above "switch on".
            IBlockExtension extension = BlockExtensions.For(block);
            if (extension != null)
                extension.Describe(kind);

            kind.WithProperty(GridEntity.WorkingProperty, "working")
                .WithState("is-working", GridEntity.WorkingProperty, true, "is working")
                .WithState("is-not-working", GridEntity.WorkingProperty, false, "is not working");

            // Only a block with a switch gets the words for one. A cargo container has no on and off, and
            // offering them would be offering a sentence that cannot come true.
            if (block is IMyFunctionalBlock)
            {
                kind.WithProperty(GridEntity.OnProperty, "switched on")
                    .WithEvent("switched-on", GridEntity.OnProperty, Edge.Rising, "is switched on")
                    .WithEvent("switched-off", GridEntity.OnProperty, Edge.Falling, "is switched off")
                    .WithState("is-on", GridEntity.OnProperty, true, "is on")
                    .WithState("is-off", GridEntity.OnProperty, false, "is off")
                    .WithAction(SwitchOn, "switch on", "is-on")
                    .WithAction(SwitchOff, "switch off", "is-off");
            }

            _kinds.Add(id, kind);
            return kind;
        }
    }
}
