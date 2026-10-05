using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The blueprints of the logic systems a construct had when it lost its automation, and which construct that was.</summary>
    public sealed class Backup
    {
        /// <summary>The blueprints <paramref name="program" /> of the construct called <paramref name="name" />, made of <paramref name="grids" />.</summary>
        public Backup(string name, DateTime saved, IList<long> grids, string program)
        {
            Name = name;
            Saved = saved;
            Grids = new List<long>(grids);
            Program = program;
        }

        /// <summary>What the construct was called when the copy was taken.</summary>
        public string Name { get; private set; }

        /// <summary>When the copy was taken, in UTC.</summary>
        public DateTime Saved { get; private set; }

        /// <summary>The entity ids of the construct's grids when the copy was taken, by which it is known again.</summary>
        public List<long> Grids { get; private set; }

        /// <summary>The logic systems, written out as blueprints.</summary>
        public string Program { get; private set; }

        /// <summary>Whether the construct made of <paramref name="grids" /> is the one this was taken of: whether any grid is one it had.</summary>
        public bool Of(IList<long> grids)
        {
            for (var i = 0; i < grids.Count; i++)
            {
                if (Grids.Contains(grids[i]))
                    return true;
            }

            return false;
        }

        /// <summary>Takes the copy again, from the construct as it is now.</summary>
        public void Retake(string name, DateTime saved, IList<long> grids, string program)
        {
            Name = name;
            Saved = saved;
            Grids.Clear();
            Grids.AddRange(grids);
            Program = program;
        }
    }
}
