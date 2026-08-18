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
        public BlockFilter(string label, bool isCurrent, int command)
        {
            Label = label;
            IsCurrent = isCurrent;
            Command = command;
        }

        public string Label { get; private set; }

        public bool IsCurrent { get; private set; }

        public int Command { get; private set; }
    }

    /// <summary>
    /// Narrows the list beneath it, by text and by kind.
    /// </summary>
    /// <remarks>
    /// Not optional polish. A real construct has hundreds of blocks, and a correct
    /// list nobody can navigate is no better than a wrong one. The kind filters are
    /// built from what is actually present rather than from a fixed catalogue.
    /// </remarks>
    public sealed class BlockSearchComponent : EditorComponent
    {
        string _text;
        int _command;

        public BlockSearchComponent(string text, int command)
        {
            _text = text;
            _command = command;
            Filters = new ObservableList<BlockFilter>();
        }

        /// <summary>Current search text.</summary>
        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, "Text"); }
        }

        /// <summary>Send the new text here when the user types.</summary>
        public int Command
        {
            get { return _command; }
            set { Set(ref _command, value, "Command"); }
        }

        public ObservableList<BlockFilter> Filters { get; private set; }
    }

    /// <summary>How a block list behaves when a row is acted on.</summary>
    public enum BlockListMode
    {
        /// <summary>Acting on a row drills into it.</summary>
        Navigate,

        /// <summary>Rows are picked and unpicked, several at a time.</summary>
        Choose
    }

    /// <summary>One block in a list.</summary>
    public sealed class BlockEntry : Observable
    {
        string _status;
        bool _isActive;
        int _sequenceCount;

        public BlockEntry(long deviceId, string name, string kind, int command)
        {
            DeviceId = deviceId;
            Name = name;
            Kind = kind;
            Command = command;
        }

        public long DeviceId { get; private set; }

        public string Name { get; private set; }

        public string Kind { get; private set; }

        public int Command { get; private set; }

        /// <summary>Picked, in <see cref="BlockListMode.Choose"/>.</summary>
        public bool IsChosen { get; set; }

        /// <summary>Live state, in the user's words: "closed", "opening…", "62%".</summary>
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value, "Status"); }
        }

        /// <summary>Whether it is doing something right now.</summary>
        public bool IsActive
        {
            get { return _isActive; }
            set { Set(ref _isActive, value, "IsActive"); }
        }

        /// <summary>
        /// How many sequences touch this block, so "what is affecting this?" is
        /// answerable by looking rather than by reading every sequence.
        /// </summary>
        public int SequenceCount
        {
            get { return _sequenceCount; }
            set { Set(ref _sequenceCount, value, "SequenceCount"); }
        }
    }

    /// <summary>
    /// Blocks on the construct, or the subset that can fill a slot.
    /// </summary>
    /// <remarks>
    /// In <see cref="BlockListMode.Choose"/> only blocks that would leave the sequence
    /// sayable appear at all, so an impossible combination is unreachable rather than
    /// rejected. Picking several is the ordinary case, which is what keeps iteration
    /// out of the language entirely.
    /// </remarks>
    public sealed class BlockListComponent : EditorComponent
    {
        string _emptyText;

        public BlockListComponent(BlockListMode mode)
        {
            Mode = mode;
            Entries = new ObservableList<BlockEntry>();
        }

        public BlockListMode Mode { get; private set; }

        public ObservableList<BlockEntry> Entries { get; private set; }

        /// <summary>Shown instead of the list when nothing matches.</summary>
        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }

    /// <summary>One option in a choice list.</summary>
    public sealed class ChoiceEntry
    {
        public ChoiceEntry(string text, bool isCurrent, int command)
        {
            Text = text;
            IsCurrent = isCurrent;
            Command = command;
        }

        public string Text { get; private set; }

        public bool IsCurrent { get; private set; }

        public int Command { get; private set; }

        /// <summary>
        /// How often, in ticks, this option's indicator should blink - or 0 for none.
        /// </summary>
        /// <remarks>
        /// Used by the repeat rates, where the label alone asks the reader to imagine a
        /// speed. A dot blinking at the actual rate is the thing itself, and an
        /// interval of one tick simply reads as constantly lit - which is exactly what
        /// it means. The words stay; the blink makes them concrete.
        /// </remarks>
        public int BlinkTicks { get; set; }

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

    /// <summary>One sequence, summarised to a line.</summary>
    public sealed class SequenceEntry : Observable
    {
        string _summary;
        string _status;
        string _lastRun;
        string _problem;
        bool _pulse;

        public SequenceEntry(int sequenceId, string summary, int command)
        {
            SequenceId = sequenceId;
            _summary = summary;
            Command = command;
        }

        public int SequenceId { get; private set; }

        public int Command { get; private set; }

        /// <summary>The sequence read as one line - its name, or its whole sentence.</summary>
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

        /// <summary>Lit briefly each time it fires - the visible heartbeat.</summary>
        public bool Pulse
        {
            get { return _pulse; }
            set { Set(ref _pulse, value, "Pulse"); }
        }

        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, "Problem"); }
        }
    }

    /// <summary>Sequences, one line each.</summary>
    public sealed class SequenceListComponent : EditorComponent
    {
        string _emptyText;

        public SequenceListComponent()
        {
            Entries = new ObservableList<SequenceEntry>();
        }

        public ObservableList<SequenceEntry> Entries { get; private set; }

        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }
}
