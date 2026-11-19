using System;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing
{
    /// <summary>What the Share menu of one blueprint or logic system offers: the dialog for sharing it, and its text to copy.</summary>
    public sealed class ShareOffer
    {
        /// <summary>What the first item does, opening the dialog for sharing it, or null where it cannot be shared in this world.</summary>
        public ICommand Shares { get; set; }

        /// <summary>The words of the first item.</summary>
        public string SharesText { get; set; }

        /// <summary>What the first item does, in markdown-lite, said where the pointer rests on it.</summary>
        public string SharesAbout { get; set; }

        /// <summary>What it is called, which the compact copy carries; asked as an item is pressed.</summary>
        public Func<string> FnName { get; set; }

        /// <summary>It written as a blueprint, which the copies put on the clipboard; asked as an item is pressed.</summary>
        public Func<string> FnText { get; set; }
    }
}
