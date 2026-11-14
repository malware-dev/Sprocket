using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>One player's settings in this world.</summary>
    [ProtoContract]
    public sealed class PlayerMemory
    {
        /// <summary>The player's identity.</summary>
        [ProtoMember(1)]
        public long Player { get; set; }

        /// <summary>Whether the player gives lengths of time in ticks rather than seconds.</summary>
        [ProtoMember(2)]
        public bool Ticks { get; set; }

        /// <summary>Which way the player gives a color in a color picker, as the place of its tab, counted from nought.</summary>
        [ProtoMember(3)]
        public int ColorTab { get; set; }

        /// <summary>The colors the player last chose in a color picker, as their codes, the newest first.</summary>
        [ProtoMember(4)]
        public List<string> RecentColors { get; set; } = new List<string>();

        /// <summary>Whether the pickers list the blocks hidden from toolbars: their eye open.</summary>
        [ProtoMember(5)]
        public bool ShowsHidden { get; set; }

        /// <summary>The identities of the players whose shared blueprints this player has chosen not to see.</summary>
        [ProtoMember(6)]
        public List<long> Blocked { get; set; } = new List<long>();

        /// <summary>Sprocket's clock when the player last opened the library.</summary>
        [ProtoMember(7)]
        public int SawShares { get; set; }
    }
}
