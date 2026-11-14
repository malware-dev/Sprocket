using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    /// <summary>The constructs of the world, each the grids joined by mechanical blocks, and the Sprockets on each.</summary>
    public sealed class Constructs
    {
        readonly Roster _roster;
        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();
        readonly List<SprocketUnit> _members = new List<SprocketUnit>();
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        readonly Dictionary<long, int> _sizes = new Dictionary<long, int>();

        /// <summary>The constructs of a world whose Sprockets <paramref name="roster" /> holds.</summary>
        public Constructs(Roster roster)
        {
            _roster = roster;
        }

        /// <summary>Fills <paramref name="into" /> with every Sprocket mechanically connected to <paramref name="grid" />.</summary>
        /// <remarks>Allocates nothing.</remarks>
        public void Members(IMyCubeGrid grid, List<SprocketUnit> into)
        {
            into.Clear();
            if (grid == null || MyAPIGateway.GridGroups == null)
                return;

            _group.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, _group);

            foreach (SprocketUnit unit in _roster.All)
            {
                IMyTerminalBlock owner = unit.Block;
                if (owner == null || owner.CubeGrid == null)
                    continue;

                if (_group.Contains(owner.CubeGrid))
                    into.Add(unit);
            }
        }

        /// <summary>
        /// The grid that stands for the construct <paramref name="grid" /> is part of: the oldest of its grids, the one with the
        /// lowest entity id, which every machine finds the same; or null.
        /// </summary>
        public IMyCubeGrid Founding(IMyCubeGrid grid)
        {
            if (grid == null || MyAPIGateway.GridGroups == null)
                return grid;

            _group.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, _group);

            var founding = grid;
            foreach (var member in _group)
            {
                if (member.EntityId < founding.EntityId)
                    founding = member;
            }

            return founding;
        }

        /// <summary>Fills <paramref name="into" /> with the Sprockets of the construct whose founding grid is <paramref name="construct" />, and answers its name; or null where the grid is gone.</summary>
        public string Of(long construct, List<SprocketUnit> into)
        {
            into.Clear();
            var grid = MyAPIGateway.Entities?.GetEntityById(construct) as IMyCubeGrid;
            if (grid == null)
                return null;

            Members(grid, into);
            return Name(into);
        }

        /// <summary>What the construct <paramref name="grid" /> is part of is called: the name of its largest grid, or empty for a construct with no automation.</summary>
        public string Name(IMyCubeGrid grid)
        {
            Members(grid, _members);
            return Name(_members);
        }

        /// <summary>
        /// What a player turned away from the construct <paramref name="grid" /> is part of is told it is called: its name, or, where
        /// this machine holds no Sprocket of it, as a client turned away holds none, the name of its largest grid worked out from its
        /// grids.
        /// </summary>
        /// <remarks>Counts the blocks of every grid of such a construct, so it is for a refusal, never a repeating path.</remarks>
        public string Called(IMyCubeGrid grid)
        {
            Members(grid, _members);
            if (_members.Count > 0)
                return Name(_members);

            // The grids of the construct, as Members found them.
            if (grid == null || MyAPIGateway.GridGroups == null)
                return "";

            _sizes.Clear();
            foreach (var member in _group)
            {
                _blocks.Clear();
                member.GetBlocks(_blocks);
                _sizes[member.EntityId] = _blocks.Count;
            }

            var largest = Largest(_group, _sizes);
            return largest == null ? "" : largest.CustomName ?? "";
        }

        /// <summary>What the construct with the Sprockets <paramref name="members" /> on it is called: the name of its largest grid, or empty for a construct with no automation.</summary>
        public static string Name(List<SprocketUnit> members)
        {
            return members.Count == 0 ? "" : members[0].World.Name;
        }

        /// <summary>
        /// Which of <paramref name="grids" />, their block counts in <paramref name="sizes" /> by entity id, is the largest: the
        /// one holding the most blocks, and of several holding as many, the first in the order the group gives them; or null
        /// where there are none.
        /// </summary>
        /// <remarks>
        /// The game's own rule, from its list of entities, which shows a mechanical group as its grid with the most blocks and,
        /// of several holding as many, the first it comes to.
        /// </remarks>
        public static IMyCubeGrid Largest(List<IMyCubeGrid> grids, Dictionary<long, int> sizes)
        {
            IMyCubeGrid largest = null;
            var most = 0;
            foreach (var grid in grids)
            {
                int size;
                sizes.TryGetValue(grid.EntityId, out size);
                if (largest != null && size <= most)
                    continue;

                largest = grid;
                most = size;
            }

            return largest;
        }
    }
}
