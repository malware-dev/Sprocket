using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Messages
{
    /// <summary>One share, as the server tells a client of it.</summary>
    public sealed class ShareLine
    {
        /// <summary>The identity of the player who shared it.</summary>
        public long Sharer;

        /// <summary>That player's name.</summary>
        public string SharerName = "";

        /// <summary>The blueprint's identity.</summary>
        public string Identity = "";

        /// <summary>What the blueprint is called.</summary>
        public string Name = "";

        /// <summary>Who it is for.</summary>
        public ShareReach Reach;

        /// <summary>The identities of the players it is for, separated by spaces, where <see cref="Reach" /> names players.</summary>
        public string Players = "";

        /// <summary>Whether it has changed since this player last opened the library.</summary>
        public bool IsNew;

        /// <summary>The blueprint, where the client is to use it, or empty where it is only listed.</summary>
        public string Blueprint = "";
    }
}
