namespace Reactor.Ui
{
    /// <summary>
    /// One high-level piece of the editor.
    /// </summary>
    /// <remarks>
    /// These are specialized and domain-shaped on purpose. A generic vocabulary of
    /// buttons, rows and labels would force every target to reassemble a list of
    /// blocks out of primitives, and would drag layout into core - which is the one
    /// place it must never live.
    ///
    /// Components are long-lived. A page creates them once and thereafter changes
    /// their values and their lists, which is what lets a target build its controls
    /// once and keep them - along with focus, carets and scroll positions.
    ///
    /// The set is kept small deliberately. Every component added is work multiplied
    /// by the number of targets, so a new one has to earn its place.
    /// </remarks>
    public abstract class EditorComponent : Observable
    {
        bool _fills;

        /// <summary>
        /// Whether this component should take whatever space is left and scroll
        /// inside itself, rather than the screen scrolling as a whole.
        /// </summary>
        /// <remarks>
        /// Set on the one long list a screen is really about. Without it a target
        /// scrolls the entire screen, which drags the search box and the filters off
        /// the top along with the rows they are filtering.
        ///
        /// At most one component per screen should set this. A screen with none - a
        /// form, a page of sections - simply scrolls as a whole, which is right for it.
        /// </remarks>
        public bool Fills
        {
            get { return _fills; }
            set { Set(ref _fills, value, "Fills"); }
        }
    }

    /// <summary>A label introducing the section under it.</summary>
    public sealed class HeadingComponent : EditorComponent
    {
        string _text;

        public HeadingComponent(string text)
        {
            _text = text;
        }

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, "Text"); }
        }
    }

    public enum NoticeLevel
    {
        /// <summary>Worth reading, nothing is wrong.</summary>
        Info,

        /// <summary>Something is wrong and the user has to know about it.</summary>
        Problem
    }

    /// <summary>
    /// Something the user needs told.
    /// </summary>
    /// <remarks>
    /// Used above all for broken references. A sequence quietly doing nothing is
    /// indistinguishable from one that works, which is the exact shape of failure a
    /// non-programmer cannot diagnose - so it is never silent.
    /// </remarks>
    public sealed class NoticeComponent : EditorComponent
    {
        string _text;
        NoticeLevel _level;

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

        public NoticeLevel Level
        {
            get { return _level; }
            set { Set(ref _level, value, "Level"); }
        }
    }

    /// <summary>One kind filter offered beside a search box.</summary>
    public sealed class BlockFilter
    {
        public BlockFilter(string label, int count, bool isCurrent, int command)
        {
            Label = label;
            Count = count;
            IsCurrent = isCurrent;
            Command = command;
        }

        public string Label { get; set; }

        /// <summary>
        /// How many there are of this kind.
        /// </summary>
        /// <remarks>
        /// A fact rather than a decoration. Which kinds are worth reaching for depends on how
        /// much is behind each one - fourteen doors is a useful narrowing, one antenna is not -
        /// and a target that has room to say so should be able to.
        /// </remarks>
        public int Count { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }
    }

    /// <summary>One option in a choice list.</summary>
    /// <remarks>
    /// Settable throughout, so a page that already has one can change what it says rather than
    /// making another. Rebuilding is what loses whatever somebody was doing with the thing.
    /// </remarks>
    public sealed class ChoiceEntry
    {
        public ChoiceEntry(string text, bool isCurrent, int command)
        {
            Text = text;
            IsCurrent = isCurrent;
            Command = command;
        }

        public string Text { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }

        /// <summary>
        /// The interval this option stands for, in ticks, or 0 if it stands for no interval.
        /// </summary>
        /// <remarks>
        /// Used by the repeat rates, where the label alone asks the reader to imagine a speed.
        /// Knowing the actual number lets a target show it rather than describe it - this host
        /// blinks a dot at exactly that rate, so "every ten ticks" is a thing you can see
        /// instead of a thing you have to picture. Whether to do that, and how, is its own
        /// decision; core supplies the number.
        /// </remarks>
        public int IntervalTicks { get; set; }

        /// <summary>
        /// What picking this will cost, in plain words, or empty.
        /// </summary>
        /// <remarks>
        /// Shown beside the option rather than after choosing it. A warning that
        /// arrives once the damage is done is not a warning.
        /// </remarks>
        public string Warning { get; set; }
    }

    /// <summary>
    /// A short list of named options - what can happen, what to do, how often.
    /// </summary>
    /// <remarks>
    /// Short by construction: it only ever contains what the blocks in the sequence
    /// can actually do, so there is never an invalid option to offer or explain.
    /// </remarks>
    public sealed class ChoiceListComponent : EditorComponent
    {
        string _emptyText;

        public ChoiceListComponent()
        {
            Entries = new ObservableList<ChoiceEntry>();
        }

        public ObservableList<ChoiceEntry> Entries { get; private set; }

        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }

    /// <summary>One thing that is doing something, summarised to a line.</summary>
    public sealed class StatusEntry : Observable
    {
        string _summary;
        string _status;
        string _lastRun;
        string _problem;
        bool _ranLight;

        public StatusEntry(string summary, int command)
        {
            _summary = summary;
            Command = command;
        }

        /// <summary>
        /// How far in this row sits: 0 for a thing, 1 for one of that thing.
        /// </summary>
        /// <remarks>
        /// One level, and there will never be more. A machine and the ones of it fitted to a
        /// ship is the whole of the nesting this language has, so a target can indent by a
        /// fixed amount rather than reinventing a tree.
        /// </remarks>
        public int Depth { get; set; }

        /// <summary>
        /// Whether this row does something rather than being something.
        /// </summary>
        /// <remarks>
        /// "Another one of these" belongs in the list, under the machine it would add to,
        /// rather than somewhere else on the screen where it would have to explain which
        /// machine it meant. It is not a thing with a state, so a target should draw it
        /// quieter than its neighbours.
        /// </remarks>
        public bool IsAction { get; set; }

        public int Command { get; private set; }

        /// <summary>Read as one line - a name, or a whole sentence.</summary>
        public string Summary
        {
            get { return _summary; }
            set { Set(ref _summary, value, "Summary"); }
        }

        /// <summary>Live: waiting, running, switched off.</summary>
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value, "Status"); }
        }

        /// <summary>When it last ran, in words. Empty if it never has.</summary>
        public string LastRun
        {
            get { return _lastRun; }
            set { Set(ref _lastRun, value, "LastRun"); }
        }

        /// <summary>
        /// Whether it ran a moment ago.
        /// </summary>
        /// <remarks>
        /// The fact, not the flash. A target that wants a heartbeat has one; a target that
        /// would rather say so in words is not being told to blink.
        /// </remarks>
        public bool JustRan
        {
            get { return _ranLight; }
            set { Set(ref _ranLight, value, "JustRan"); }
        }

        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, "Problem"); }
        }
    }

    /// <summary>
    /// Named things that are doing something, one line each.
    /// </summary>
    /// <remarks>
    /// Serves the machines on a ship and the sequences inside one, because they are the same
    /// thing to look at: a name, what it is doing now, when it last did anything, and whether
    /// something is wrong with it.
    ///
    /// That is not a slide towards generic widgets. It is still shaped by this domain - live
    /// automation with a heartbeat and a problem - rather than being a list control that
    /// would leave every target reassembling one out of primitives.
    /// </remarks>
    public sealed class StatusListComponent : EditorComponent
    {
        string _emptyText;

        public StatusListComponent()
        {
            Entries = new ObservableList<StatusEntry>();
        }

        public ObservableList<StatusEntry> Entries { get; private set; }

        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }
}
