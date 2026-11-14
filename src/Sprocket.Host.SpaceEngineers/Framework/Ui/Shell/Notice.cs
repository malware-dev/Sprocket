using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>Something the person at the editor needs told above whatever page is showing.</summary>
    public class Notice
    {
        /// <summary>The one line it always says.</summary>
        public string Text { get; set; } = "";

        /// <summary>The rest of it, shown only while <see cref="Expanded" />, or empty.</summary>
        public string Detail { get; set; } = "";

        /// <summary>The technical reading, shown under the detail.</summary>
        public string Diagnostic { get; set; } = "";

        /// <summary>Whether it is a problem or only news.</summary>
        public NoticeLevel Level { get; set; }

        /// <summary>Whether the detail and the offer are on show.</summary>
        public bool Expanded { get; set; }

        /// <summary>What opening or closing it does, or null where there is nothing to open.</summary>
        public ICommand Expands { get; set; }

        /// <summary>What the offer says, shown with the detail.</summary>
        public string OfferText { get; set; } = "";

        /// <summary>What the offer does, or null for a notice that is only news.</summary>
        public ICommand Offers { get; set; }

        /// <summary>Whether it should slide out.</summary>
        /// <remarks>Set by whoever raised it, in place of dropping it; it may be dropped once <see cref="Closed" /> is set.</remarks>
        public bool Leaving { get; set; }

        /// <summary>Whether it has finished sliding out.</summary>
        /// <remarks>Written by whatever is drawing it.</remarks>
        public bool Closed { get; set; }
    }
}
