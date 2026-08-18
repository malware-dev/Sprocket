using System.Collections.Generic;

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
    /// Instead a target implements a renderer per component and owns everything
    /// about how it looks, measures and arranges. Space Engineers can hand a
    /// <see cref="BlockListComponent"/> straight to its own list control; a desktop
    /// host can use a virtualizing list; a text-mode host can print it. None of them
    /// have to agree on anything but the data.
    ///
    /// The set is kept small deliberately. Every component added is work multiplied
    /// by the number of targets, so a new one has to earn its place.
    /// </remarks>
    public abstract class EditorComponent
    {
    }

    /// <summary>A label introducing the section under it.</summary>
    public sealed class HeadingComponent : EditorComponent
    {
        public HeadingComponent(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
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
    /// indistinguishable from a sequence that works, which is the exact shape of failure
    /// a non-programmer cannot diagnose - so it is never silent.
    /// </remarks>
    public sealed class NoticeComponent : EditorComponent
    {
        public NoticeComponent(string text, NoticeLevel level)
        {
            Text = text;
            Level = level;
        }

        public string Text { get; set; }

        public NoticeLevel Level { get; set; }
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

        public string Label { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }
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
        public BlockSearchComponent(string text, int command)
        {
            Text = text;
            Command = command;
            Filters = new List<BlockFilter>();
        }

        /// <summary>Current search text.</summary>
        public string Text { get; set; }

        /// <summary>Send the new text here when the user types.</summary>
        public int Command { get; set; }

        public List<BlockFilter> Filters { get; private set; }
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
    public sealed class BlockEntry
    {
        public BlockEntry(long deviceId, string name, string kind, int command)
        {
            DeviceId = deviceId;
            Name = name;
            Kind = kind;
            Command = command;
        }

        public long DeviceId { get; set; }

        public string Name { get; set; }

        public string Kind { get; set; }

        /// <summary>Live state, in the user's words: "closed", "opening…", "62%".</summary>
        public string Status { get; set; }

        /// <summary>Whether it is doing something right now.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// How many sequences touch this block, so "what is affecting this?" is
        /// answerable by looking rather than by reading every sequence.
        /// </summary>
        public int SequenceCount { get; set; }

        /// <summary>Picked, in <see cref="BlockListMode.Choose"/>.</summary>
        public bool IsChosen { get; set; }

        public int Command { get; set; }
    }

    /// <summary>
    /// Blocks on the construct, or the subset that can fill a slot.
    /// </summary>
    /// <remarks>
    /// In <see cref="BlockListMode.Choose"/> only blocks that would leave the sequence
    /// sayable appear at all, so an impossible combination is unreachable rather
    /// than rejected. Picking several is the ordinary case, which is what keeps
    /// iteration out of the language entirely.
    /// </remarks>
    public sealed class BlockListComponent : EditorComponent
    {
        public BlockListComponent(BlockListMode mode)
        {
            Mode = mode;
            Entries = new List<BlockEntry>();
        }

        public BlockListMode Mode { get; set; }

        public List<BlockEntry> Entries { get; private set; }

        /// <summary>Shown instead of the list when nothing matches.</summary>
        public string EmptyText { get; set; }
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

        public string Text { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }

        /// <summary>
        /// How often, in ticks, this option's indicator should blink - or 0 for no
        /// indicator at all.
        /// </summary>
        /// <remarks>
        /// Used by the repeat rates, where the label alone asks the reader to imagine
        /// a speed. A dot blinking at the actual rate is the thing itself, and an
        /// interval of one tick simply reads as constantly lit - which is exactly
        /// what it means. The words stay; the blink is what makes them concrete.
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
    /// Short by construction: it only ever contains what the blocks in the sequence can
    /// actually do, so there is never an invalid option to offer or explain.
    /// </remarks>
    public sealed class ChoiceListComponent : EditorComponent
    {
        public ChoiceListComponent()
        {
            Entries = new List<ChoiceEntry>();
        }

        public List<ChoiceEntry> Entries { get; private set; }

        public string EmptyText { get; set; }
    }

    /// <summary>One sequence, summarised to a line.</summary>
    public sealed class SequenceEntry
    {
        public SequenceEntry(int sequenceId, string summary, int command)
        {
            SequenceId = sequenceId;
            Summary = summary;
            Command = command;

            // Not zero: zero is a real key ("just now"), so a fresh entry has to start
            // at a value no computed key can equal, or the first update is skipped.
            LastRunKey = int.MinValue;
        }

        public int SequenceId { get; set; }

        /// <summary>The sequence read as one sentence.</summary>
        public string Summary { get; set; }

        /// <summary>Live: waiting, running, last fired.</summary>
        public string Status { get; set; }

        /// <summary>Lit briefly each time it fires - the visible heartbeat.</summary>
        public bool Pulse { get; set; }

        /// <summary>When it last ran, in words: "just now", "3 minutes ago", or empty.</summary>
        public string LastRun { get; set; }

        /// <summary>
        /// Bookkeeping, not for display: the key <see cref="LastRun"/> was built from,
        /// so the words are only rebuilt when they would actually differ.
        /// </summary>
        public int LastRunKey { get; set; }

        public string Problem { get; set; }

        public int Command { get; set; }
    }

    /// <summary>Sequences, one line each.</summary>
    public sealed class SequenceListComponent : EditorComponent
    {
        public SequenceListComponent()
        {
            Entries = new List<SequenceEntry>();
        }

        public List<SequenceEntry> Entries { get; private set; }

        public string EmptyText { get; set; }
    }
}
