using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>The blueprint of one logic system a construct had when it lost its automation, and which construct that was.</summary>
    public sealed class Backup
    {
        /// <summary>The blueprint in <paramref name="file" /> of the logic system <paramref name="name" />, stored of the construct <paramref name="construct" />.</summary>
        public Backup(string file, Guid identity, string name, string construct, DateTime lost)
        {
            File = file;
            Identity = identity;
            Name = name;
            Construct = construct;
            Lost = lost;
        }

        /// <summary>The file in world storage the blueprint is stored in.</summary>
        public string File { get; }

        /// <summary>The logic system's identity.</summary>
        public Guid Identity { get; }

        /// <summary>The logic system's name.</summary>
        public string Name { get; }

        /// <summary>What the construct was called when it lost its automation, a label of where the backup came from.</summary>
        public string Construct { get; }

        /// <summary>When the construct lost its automation, in UTC.</summary>
        public DateTime Lost { get; }
    }
}
