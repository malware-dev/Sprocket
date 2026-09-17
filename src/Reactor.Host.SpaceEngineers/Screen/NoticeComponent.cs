using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Lists;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Something the user needs told, and what they can do about it.
    /// </summary>
    /// <remarks>
    /// Used above all for broken references. A sequence quietly doing nothing is
    /// indistinguishable from one that works, which is the exact shape of failure a
    /// non-programmer cannot diagnose - so it is never silent.
    /// <para>
    /// One line by default. <see cref="Detail" /> is what somebody gets when they ask for it, because a
    /// banner that says everything at once is a banner that takes a third of the screen to say something
    /// most people already understand - and the ones who do not are the ones who will open it.
    /// </para>
    /// </remarks>
    public sealed class NoticeComponent : EditorComponent
    {
        readonly List<ChoiceEntry> _actions = new List<ChoiceEntry>();

        string _text;
        string _detail = "";
        string _diagnostic = "";
        NoticeLevel _level;
        bool _expanded;
        int _expandCommand;
        bool _leaving;

        public NoticeComponent(string text, NoticeLevel level)
        {
            _text = text;
            _level = level;
        }

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, "Text"); }
        }

        /// <summary>The rest of it, shown only while <see cref="Expanded" />. Empty where there is none.</summary>
        public string Detail
        {
            get { return _detail; }
            set { Set(ref _detail, value ?? "", "Detail"); }
        }

        public NoticeLevel Level
        {
            get { return _level; }
            set { Set(ref _level, value, "Level"); }
        }

        /// <summary>Whether the detail and the operations are on show.</summary>
        public bool Expanded
        {
            get { return _expanded; }
            set { Set(ref _expanded, value, "Expanded"); }
        }

        /// <summary>What to invoke to open or close it, or nothing where there is nothing to open.</summary>
        public int ExpandCommand
        {
            get { return _expandCommand; }
            set { Set(ref _expandCommand, value, "ExpandCommand"); }
        }

        /// <summary>
        /// The technical reading, shown under the detail and in its own voice.
        /// </summary>
        /// <remarks>
        /// Kept apart from <see cref="Detail" /> rather than added to the end of it. A parser's words
        /// are written for whoever is mending Reactor, and a sentence of plain English that trails off
        /// into them reads as though the explanation broke halfway through - which is how the first
        /// attempt at this banner read. On a line of its own, quieter, it is a reading rather than an
        /// interruption, and the person who wants it knows it when they see it.
        /// </remarks>
        public string Diagnostic
        {
            get { return _diagnostic; }
            set { Set(ref _diagnostic, value ?? "", "Diagnostic"); }
        }

        /// <summary>
        /// What can be done about it from here, shown with the detail.
        /// </summary>
        /// <remarks>
        /// A notice about something a person can mend should carry the mending, because the alternative
        /// is a sentence telling them to go and find a screen. Empty for anything that is only news.
        /// </remarks>
        public IList<ChoiceEntry> Actions => _actions;

        /// <summary>
        /// Whether this has said what it came to say and should slide out.
        /// </summary>
        /// <remarks>
        /// Set by whoever raised it, rather than dropping the notice outright: a banner that is simply
        /// taken out of the list disappears between one frame and the next, and everything under it jumps
        /// up to fill the gap. This says "stop showing this, in your own time".
        /// </remarks>
        public bool Leaving
        {
            get { return _leaving; }
            set { Set(ref _leaving, value, "Leaving"); }
        }

        /// <summary>
        /// Whether it has finished sliding out, so it may now be dropped.
        /// </summary>
        /// <remarks>
        /// <b>Written by whatever is drawing it</b>, which is the one thing that knows how long a slide
        /// takes and how many frames it has had. The alternative is a count of ticks agreed between the
        /// two, which is the same number written down twice and drifting.
        /// </remarks>
        public bool Closed { get; set; }
    }
}
