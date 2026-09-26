using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Something the person at the editor needs told, above whatever page is showing, and what they can do about it.
    /// </summary>
    /// <remarks>
    /// One line by default. <see cref="Detail" /> is what somebody gets when they ask for it, because a
    /// banner that says everything at once is a banner that takes a third of the screen to say something
    /// most people already understand - and the ones who do not are the ones who will open it.
    /// </remarks>
    public class Notice
    {
        /// <summary>The one line it always says.</summary>
        public string Text { get; set; } = "";

        /// <summary>The rest of it, shown only while <see cref="Expanded" />. Empty where there is none.</summary>
        public string Detail { get; set; } = "";

        /// <summary>
        /// The technical reading, shown under the detail and in its own voice.
        /// </summary>
        /// <remarks>
        /// Kept apart from <see cref="Detail" /> rather than added to the end of it. A parser's words are
        /// written for whoever is mending Sprocket, and a sentence of plain English that trails off into them
        /// reads as though the explanation broke halfway through. On a line of its own, quieter, it is a
        /// reading rather than an interruption.
        /// </remarks>
        public string Diagnostic { get; set; } = "";

        /// <summary>Whether it is a problem or only news.</summary>
        public NoticeLevel Level { get; set; }

        /// <summary>Whether the detail and the offer are on show.</summary>
        public bool Expanded { get; set; }

        /// <summary>What opening or closing it does, or null where there is nothing to open.</summary>
        public ICommand Expands { get; set; }

        /// <summary>What the offer says, shown with the detail.</summary>
        /// <remarks>
        /// A notice about something a person can mend carries the mending, because the alternative is a
        /// sentence telling them to go and find a screen.
        /// </remarks>
        public string OfferText { get; set; } = "";

        /// <summary>What the offer does, or null for a notice that is only news.</summary>
        public ICommand Offers { get; set; }

        /// <summary>
        /// Whether it has said what it came to say and should slide out.
        /// </summary>
        /// <remarks>
        /// Set by whoever raised it, rather than dropping the notice outright: a banner that simply stops
        /// being there disappears between one frame and the next, and everything under it jumps up to fill
        /// the gap.
        /// </remarks>
        public bool Leaving { get; set; }

        /// <summary>
        /// Whether it has finished sliding out, so it may now be dropped.
        /// </summary>
        /// <remarks>
        /// <b>Written by whatever is drawing it</b>, which is the one thing that knows how long a slide takes
        /// and how many frames it has had.
        /// </remarks>
        public bool Closed { get; set; }
    }
}
