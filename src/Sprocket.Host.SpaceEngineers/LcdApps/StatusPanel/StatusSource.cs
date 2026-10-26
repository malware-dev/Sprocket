using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>A construct's status worked out where its automation runs, again only when the automation has changed.</summary>
    public sealed class StatusSource
    {
        readonly List<SprocketUnit> _looked = new List<SprocketUnit>();
        long _stamp = -1;

        /// <summary>How the construct's automation stands, as it was last looked at.</summary>
        public ConstructStatus Status { get; } = new ConstructStatus();

        /// <summary>Works out how the automation of <paramref name="units" /> on the construct called <paramref name="name" /> stands at tick <paramref name="now" />, where anything about it has changed since it was last looked at.</summary>
        /// <remarks>Costs a sum over the units each time, and a lookup per sequence on each installation only when something changed.</remarks>
        public void Look(List<SprocketUnit> units, string name, int now)
        {
            var stamp = Stamp(units);
            if (stamp == _stamp && Same(units) && name == Status.Construct && Status.Heard)
                return;

            _stamp = stamp;
            _looked.Clear();
            _looked.AddRange(units);

            Status.Begin(now);
            foreach (SprocketUnit unit in units)
                Status.Include(unit.Program, unit.Runner);

            Status.End(name);
        }

        /// <summary>A number that grows whenever anything the status is worked out from changes, since every count in it only grows.</summary>
        static long Stamp(List<SprocketUnit> units)
        {
            long stamp = 0;
            foreach (SprocketUnit unit in units)
            {
                var program = unit.Program;
                var runner = unit.Runner;
                stamp += program.Revision + program.Wiring + program.Edits + program.Starts + program.Switches + runner.Revision
                    + runner.Bindings.Revision + program.PausedFrom.Count;
            }

            return stamp;
        }

        /// <summary>Whether <paramref name="units" /> are the ones there when the construct was last looked at.</summary>
        bool Same(List<SprocketUnit> units)
        {
            if (units.Count != _looked.Count)
                return false;

            for (var i = 0; i < units.Count; i++)
            {
                if (units[i] != _looked[i])
                    return false;
            }

            return true;
        }
    }
}
