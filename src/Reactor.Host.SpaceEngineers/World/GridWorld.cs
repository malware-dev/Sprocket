using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.Core.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// The construct the editor was opened on, as Reactor sees it.
    /// </summary>
    /// <remarks>
    /// <b>This is the universal floor and nothing more.</b> Every block gets the same small vocabulary -
    /// whether it is switched on, whether it is working, and the two actions that change the first. That is
    /// deliberate rather than unfinished: <c>docs/Catalogue.md</c> argues it is the one thing every block on
    /// the surface can answer, modded blocks included, and therefore the thing to build before anything that
    /// knows what a door is.
    /// <para>
    /// What is not here yet is the base layer proper, which reads a block's own terminal properties and
    /// actions, and the extensions above it.
    /// </para>
    /// </remarks>
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
        /// <summary>The values this world deals in, and the lists behind the wide ones.</summary>
        public Values Values { get; } = new Values();

        /// <summary>
        /// Bumped by <see cref="Rebuild" />, which is the only place the set changes.
        /// </summary>
        /// <remarks>
        /// <b>Only as truthful as the staleness that drives it, and that is known to be incomplete.</b>
        /// A rotor or piston head detaching takes a whole subgrid out of this construct without adding
        /// or removing a single block, and a grid splitting or merging does the same. None of those
        /// raises the events this listens to, so both this and <see cref="Entities" /> can be wrong
        /// after one - a sequence going on cycling a door that floated away.
        /// <para>
        /// Deferred rather than unknown, and must be closed before release: see
        /// <c>docs/ApiUnknowns.md</c>, question 3.
        /// </para>
        /// </remarks>
        public int Revision { get; private set; }

        public IReadOnlyList<IEntity> Entities
        {
            get
            {
                if (_stale)
                    Rebuild();

                return _entities;
            }
        }

        public EntityNoun Noun => Blocks;

        /// <summary>
        /// The construct's name, which in this game is the block's own grid's.
        /// </summary>
        /// <remarks>
        /// The grid the Reactor sits on rather than any of the ones welded to it. A construct is several
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

        public bool CanRename => _anchor != null && _anchor.CubeGrid != null;

        public void Rename(string name)
        {
            IMyCubeGrid grid = _anchor == null ? null : _anchor.CubeGrid;
            if (grid != null)
                grid.CustomName = name;
        }

        /// <summary>
        /// An id and a name, in that order.
        /// </summary>
        /// <remarks>
        /// The id is exact and dies with a blueprint; the name survives one and is the thing a player
        /// edits casually. Grid position belongs between them, because a pasted copy keeps every
        /// position and changes every id - it is not here yet because there is no value kind wide
        /// enough to write one down. See "Still to prove" in <c>docs/Script.md</c>.
        /// </remarks>
        /// <summary>
        /// Sequences nothing can currently start, by id.
        /// </summary>
        /// <remarks>
        /// Held rather than worked out, and held as the exceptions rather than the rule, because
        /// almost nothing is ever in it: a sequence started by something the game pushes needs no
        /// arranging at all, and a button that is wired stays wired. So the ordinary answer costs one
        /// lookup in an empty set.
        /// <para>
        /// Filled in by whatever owns the wiring - it knows what it arranged and for which sequence,
        /// which is exactly what makes the question answerable without reading a toolbar per ask.
        /// </para>
        /// </remarks>
        readonly HashSet<int> _unstartable = new HashSet<int>();

        /// <summary>Which declarations are not arranged, per sequence that names them.</summary>
        /// <remarks>
        /// Only ever holds the broken ones, so it is empty on a working construct and costs a lookup
        /// that fails immediately.
        /// </remarks>
        readonly Dictionary<int, List<string>> _unarranged = new Dictionary<int, List<string>>();

        public void Unstartable(List<int> sequenceIds, Dictionary<int, List<string>> sources)
        {
            _unstartable.Clear();
            for (var i = 0; i < sequenceIds.Count; i++)
                _unstartable.Add(sequenceIds[i]);

            _unarranged.Clear();
            foreach (var pair in sources)
                _unarranged[pair.Key] = pair.Value;
        }

        public void Unarranged(Sequence sequence, List<string> sources)
        {
            if (_unarranged.Count == 0)
                return;

            List<string> broken;
            if (!_unarranged.TryGetValue(sequence.Id, out broken))
                return;

            for (var i = 0; i < broken.Count; i++)
                sources.Add(broken[i]);
        }

        public bool CanStart(Sequence sequence)
        {
            return _unstartable.Count == 0 || !_unstartable.Contains(sequence.Id);
        }

        bool _asked;

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

        public void Identify(IEntity entity, List<EntityKey> keys)
        {
            keys.Clear();
            keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id)));
            keys.Add(new EntityKey(ValueKind.Text, Values.Text(entity.Name)));
        }

        public IEntity Match(IList<EntityKey> keys, out int matched)
        {
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

        IEntity Matching(EntityKey key)
        {
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
            Revision++;
            _entities.Clear();
            _byId.Clear();
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
                    GridEntity entity = new GridEntity(block, _kinds.For(block), extension);
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

        /// <summary>Stops listening to the grids this was watching.</summary>
        public void Dispose()
        {
            Watch(new List<IMyCubeGrid>());
        }
    }
}
