using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>One logic system: what starts its sequences, and the sequences themselves.</summary>
    public sealed partial class LogicSystemPage : EditorPage
    {
        const int JustRanTicks = 10;

        readonly AutomationSystem _system;
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<Trigger> _triggers = new List<Trigger>();
        readonly List<Sequence> _runs = new List<Sequence>();

        readonly List<string> _called = new List<string>();

        int _wiring = -1;
        ListField _blocksets;
        StateTab _stateWork;
        SlotPanelTab _panelWork;
        string _madeName;
        string _madeAbout;
        bool _madeOn;
        NoticeLevel _standing;

        TriggersTab _triggersWork;
        KeyedChildren<Sequence, StatusRow> _sequenceRows;

        /// <summary>The page of <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public LogicSystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        /// <inheritdoc />
        public override string Title => SystemSummary.Describe(Editor.Texts, Editor.Runner.Bindings, _system);

        /// <inheritdoc />
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].Mark = Symbols.LogicSystem;
            parts[0].MarkAbout = Editor.Texts.Codex.TermLogicSystem;
        }

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        /// <remarks>Each tab's section scrolls under the heading and the tab row, which stay where they are.</remarks>
        public override bool ScrollsItself => true;

        /// <summary>The panel a timer's rate is being given in, or null while none is open.</summary>
        public DurationPanel Rate => _triggersWork?.Rate;

        /// <summary>The logic system's annotation, or empty for a blueprint.</summary>
        public override string Subtitle => _system.IsBlueprint ? "" : _system.Annotation;

        /// <inheritdoc />
        public override string CustomName => _system.Name ?? "";

        /// <summary>Always true: a logic system can be renamed.</summary>
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.Codex.SystemNameAbout;

        /// <summary>Always true: a logic system must have a name.</summary>
        public override bool NameRequired => true;

        /// <summary>Always true: every logic system has a switch.</summary>
        public override bool CanSwitch => true;

        /// <inheritdoc />
        public override bool SwitchIsOn => _system.Enabled;

        /// <summary>The graver of what its triggers and its sequences carry, as their tabs are flagged.</summary>
        public override NoticeLevel Standing => _standing;

        /// <inheritdoc />
        public override string SwitchAbout => Editor.SwitchAbout(_system);

        /// <summary>Whether the logic system just made is unchanged since it was made.</summary>
        public override bool Untouched => _system.Name == _madeName && _system.Annotation == _madeAbout
            && _system.Enabled == _madeOn && _system.Triggers.Count == 0 && _system.Sequences.Count == 0;

        /// <summary>The command that deletes the system, asking first.</summary>
        public override ICommand Deletes => Editor.Act(CommandKind.DeleteSystem, () => Editor.DeleteSystem(_system));

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.Codex.SystemDelete;

        /// <summary>Always true: changes to the system's triggers can be walked back.</summary>
        public override bool HasHistory => true;

        /// <summary>Whether a change to the logic system can be taken back from this screen.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_system, _system);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system, _system);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_system, _system);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system, _system);

        /// <summary>Whether what the system does may be changed now: not while it may run, and not while somebody else holds the editor.</summary>
        public bool Editable => !_system.Enabled && Editor.Holder == null;

        /// <summary>Why nothing can be changed, said above what it is about; empty while it can.</summary>
        public string LockedText => Editable
            ? ""
            : Editor.Holder != null
                ? Template.Fill(Editor.Texts.SequenceHeld, "holder", Editor.Holder)
                : Template.Fill(Editor.Texts.SystemLocked, "name", _system.Name);

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = NameRules.Clean(name);

            if (wanted.Length == 0 || !AcceptsName(wanted))
                return;

            Editor.Do(_system, new RenameSystem(_system.Name, wanted));
        }

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, NameRules.Clean(name));
            return other == null || other == _system;
        }

        /// <inheritdoc />
        public override void Switch(bool on)
        {
            Editor.SwitchSystem(_system, on);
        }

        /// <summary>The box a blueprint's description is written in.</summary>
        public Composer About => _slotPanel.Composer;

        /// <summary>The blueprint's slots, one row each, with the shown installation's blocks for each.</summary>
        public Element.Collection Slots => _slotPanel.Slots.Children;

        /// <summary>Writes the description being typed, and asks whether to keep what was changed here, on the way out.</summary>
        public override bool Leaving()
        {
            _panelWork?.Leave();
            return base.Leaving();
        }

        /// <inheritdoc />
        public override void Unmake()
        {
            Editor.TakeOut(_system);
        }

        /// <summary>Notes the logic system just made, as it was made.</summary>
        protected override void Remember()
        {
            _madeName = _system.Name;
            _madeAbout = _system.Annotation;
            _madeOn = _system.Enabled;
        }

        /// <inheritdoc />
        public override bool IsAbout(AutomationSystem system)
        {
            return system == _system;
        }

        /// <inheritdoc />
        public override void Undo()
        {
            _panelWork?.Settle();
            Editor.Undo(_system, _system);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            _panelWork?.Settle();
            Editor.Redo(_system, _system);
        }

        /// <summary>Brings the view up to date, words and all.</summary>
        public override void Build()
        {
            _panelWork?.Refocused();
            Collect();
            Show(true);
        }

        /// <summary>Brings the view up to what the sequences are doing, and asks for the screen to be built again when the program has changed.</summary>
        /// <remarks>Called every tick the page is showing.</remarks>
        public override void Refresh()
        {
            _panelWork?.Refocused();

            if (_wiring != Editor.Program.Wiring)
            {
                Editor.Invalidate();
                return;
            }

            Show(false);
        }

        /// <summary>Keeps the rows by trigger and by sequence, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _triggersWork = new TriggersTab(Editor, _system, _triggerList, Changes, Locked);
            _sequenceRows = new KeyedChildren<Sequence, StatusRow>(_sequenceList.Children, Own);
            _stateWork = new StateTab(Editor, _system, _statePanel, Changes, Locked);
            _panelWork = new SlotPanelTab(Editor, _system, _slotPanel, Changes, Locked);

            int shown;
            if (Editor.Habits.Tabs.TryGetValue(_system.Id, out shown))
                _tabs.SelectedIndex = shown;

            _tabs.SelectionChanged += at => Editor.Habits.Tabs[_system.Id] = at;

            Show(true);
        }

        void Show(bool reshaped)
        {
            // Live state is read every tick; faults are said again on the tick the runs change.

            if (_triggersWork == null)
                return;

            var texts = Editor.Texts;

            var recount = Revised() || reshaped;

            if (reshaped)
                Arrange(texts);

            var through = Editor.StandIn(_system);
            var tick = Editor.CurrentTick;

            if (recount)
                Stopped(through);

            var triggered = NoticeLevel.Info;
            var sequenced = NoticeLevel.Info;
            for (var i = 0; i < _triggers.Count; i++)
            {
                var row = _triggersWork.Row(_triggers[i]);
                var poller = Editor.Runner.Watches.Poller(_triggers[i], through);
                row.IsLiveNow = poller != null && poller.Live;

                if (recount)
                {
                    var fault = _runs[i] == null ? SequenceFault.None : Fault(_runs[i], through);
                    var own = _triggers[i].Acts != null && fault.Exists;
                    row.Problem = own || IsStarting(fault) && Concerns(_triggers[i], fault)
                        ? SequenceText.Problem(texts, fault, _runs[i])
                        : "";
                    row.Level = SequenceText.Level(fault);

                    if (Editor.Starts.Copied(_triggers[i]))
                    {
                        row.Problem = texts.TriggerCopiedButton;
                        row.Level = NoticeLevel.Info;
                    }
                }

                if (row.Problem.Length > 0)
                    triggered = Worse(triggered, row.Level);

                row.Show(Editor.World, texts);
            }

            foreach (var sequence in _sequences)
            {
                var row = _sequenceRows[sequence];

                Live(row, sequence, tick);

                if (reshaped)
                    row.Summary = SequenceText.Called(texts, sequence);

                if (recount)
                    Mark(texts, row, Fault(sequence, through), sequence);

                if (!string.IsNullOrEmpty(row.Problem))
                    sequenced = Worse(sequenced, row.Level);

                row.Show(texts);
            }

            Flag(_triggersTab, _triggersMark, triggered, texts);
            Flag(_sequencesTab, _sequencesMark, sequenced, texts);
            _standing = Worse(triggered, sequenced);

            if (_tabs.SelectedIndex >= 0 && _tabs.SelectedIndex < _tabs.Tabs.Count && _tabs.Tabs[_tabs.SelectedIndex] == _stateTab)
                _stateWork.Live();

            _triggersWork.Hang();
        }

        void Arrange(Texts texts)
        {
            // Walks the triggers' blocks for their names, so it runs only when the page is built.

            ArrangeUsing(texts);
            ArrangeLock();
            Headed();

            _triggersTab.Header = texts.SystemTriggers;
            _sequencesTab.Header = texts.SystemSequences;
            _stateTab.Header = texts.SystemState;
            _panelTab.Header = texts.SystemInstallationPanel;
            _triggersTab.Tip = CodexTip.For(_triggersTab.Tip, texts.Codex.TermTrigger);
            _sequencesTab.Tip = CodexTip.For(_sequencesTab.Tip, texts.Codex.TermSequence);
            _stateTab.Tip = CodexTip.For(_stateTab.Tip, texts.Codex.TermLogicSystemStates);
            _panelTab.Tip = CodexTip.For(_panelTab.Tip, texts.Codex.TermInstallationPanel);
            _stateWork.Arrange(texts);
            _addTrigger.Show(new AddOption(texts.SystemTrigger, null, new[]
            {
                new AddOption(SentenceText.WhenWord, texts.Codex.SystemWhenAbout,
                    Changes(CommandKind.StartWhen, () => Editor.Starts.StartSequence(_system, new WhenTrigger("")))),
                new AddOption(SentenceText.EveryWord, texts.Codex.SystemEveryAbout,
                    Changes(CommandKind.StartEvery, () => Editor.Starts.StartSequence(_system, new EveryTrigger(Editor.NewLength))))
            }));

            _runs.Clear();
            foreach (var trigger in _triggers)
            {
                _runs.Add(trigger.Acts != null ? Editor.Runner.Alone(_system, trigger) : _system.FindSequence(trigger.Runs));
            }

            _triggersWork.Arrange(_triggers);

            // A tab says it is empty only where nothing can be added to it; where something can, the ways to add it say enough.
            _noTriggersLine.IsVisible = _triggers.Count == 0 && !Editable;
            _noTriggers.Text = texts.SystemNoTriggers;

            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;
            _newSequence.Command = Changes(CommandKind.NewSequence, () => Editor.NewSequence(_system));
            _newSequence.Tip = KeptTip.For(_newSequence.Tip, _newSequence.Command != null ? null : Locked());

            _sequenceRows.Show(_sequences);
            _noSequences.IsVisible = _sequences.Count == 0 && !Editable;
            _noSequences.Text = texts.SystemNoSequences;

            foreach (var sequence in _sequences)
            {
                var row = _sequenceRows[sequence];
                row.DuplicateText = sequence.Trigger != null ? texts.Codex.SystemCopy : texts.Codex.SystemCopyUnstarted;
                row.About = texts.Codex.SystemOpenSequence;
                row.AboutArguments.Set("name", SequenceText.Called(texts, sequence));

                row.Copies = Changes(CommandKind.DuplicateSequence, () => Editor.Duplicate(sequence));
                row.Deletes = Changes(CommandKind.DeleteSequence, () => Editor.DeleteSequence(sequence));
                row.DeleteText = texts.Codex.SequenceDelete;
                row.Locked = Locked();
            }

            _panelWork.Arrange(texts);
        }

        /// <summary>
        /// Shows <paramref name="mark" /> in the color of <paramref name="level" />, saying in <paramref name="texts" /> where to look, and
        /// lays <paramref name="tab" /> on that level's wash and stripes; or hides the mark and leaves the tab plain where nothing on
        /// it is more than a note.
        /// </summary>
        static void Flag(Tab tab, Glyph mark, NoticeLevel level, Texts texts)
        {
            tab.Set(LookStates.Broken, level == NoticeLevel.Problem);
            tab.Set(LookStates.Warned, level == NoticeLevel.Warning);
            mark.IsVisible = level != NoticeLevel.Info;
            mark.Base.Tint = level == NoticeLevel.Warning ? Palette.Notice : Palette.Problem;
            mark.Tip = KeptTip.For(mark.Tip, texts.Codex.SystemTabFlagged);
        }

        /// <summary>The graver of <paramref name="one" /> and <paramref name="other" />: a problem over a warning over a note, as the levels are ordered.</summary>
        static NoticeLevel Worse(NoticeLevel one, NoticeLevel other)
        {
            return other > one ? other : one;
        }

        /// <summary>Hides the ways to add something while the system may run, and says why in their place.</summary>
        void ArrangeLock()
        {
            var editable = Editable;

            _addTriggers.IsVisible = editable;
            _addSequence.IsVisible = editable;
            _keepHere.IsVisible = editable && Editor.OpenedFrom != 0 && Editor.DeveloperMode;
            if (_keepHere.IsVisible)
            {
                var kept = _system.ListedOn.Length > 0;
                _keepHere.Text = kept ? Editor.Texts.SystemsListEverywhere : Editor.Texts.SystemsKeepHere;
                _keepHere.Tip = CodexTip.For(_keepHere.Tip, Editor.Texts.SystemsKeepHereAbout);
                _keepHere.Command = Changes(CommandKind.Expand, () => Editor.Do(_system, new ListSystemOn(_system.ListedOn,
                    kept ? "" : Editor.Program.Declare(Editor.World, Editor.World.FindEntity(Editor.OpenedFrom)).Name)));
            }

            _locked.IsVisible = !editable;
            _lockedText.Text = LockedText;
        }

        /// <summary>Says why Sprocket switched off the system, or the blockset shown, where it did.</summary>
        void Stopped(SystemInstance through)
        {
            var halt = _system.IsBlueprint
                ? through == null || through.Enabled ? null : through.Halt
                : _system.Enabled ? null : _system.Halt;

            _halted.IsVisible = halt != null;
            _halted.Text = halt == null
                ? ""
                : SequenceText.Stopped(Editor.Texts, halt, _system, SystemSummary.Problems(Editor.Runner, _system, through) == 0);
            Headed();
        }

        /// <summary>Shows the lines above the tabs only while one of them is shown, so the tabs start at the top otherwise.</summary>
        void Headed()
        {
            _head.IsVisible = _using.IsVisible || _halted.IsVisible || _locked.IsVisible;
        }

        /// <summary>Why nothing can be changed, said over each part that cannot be pressed; empty while it can.</summary>
        string Locked()
        {
            return Editable
                ? ""
                : Editor.Holder != null
                    ? Template.Fill(Editor.Texts.SequenceHeld, "holder", Editor.Holder)
                    : Editor.Texts.Codex.SystemTurnOffToEdit;
        }

        /// <summary>A command made only while the system may be changed, and null while it may not.</summary>
        ICommand Changes(CommandKind kind, Action action)
        {
            return Editable ? Editor.Act(kind, action) : null;
        }

        /// <summary>Says, for a blueprint, whose blocks the sentences name, as a list of the installations made from it.</summary>
        void ArrangeUsing(Texts texts)
        {
            var shown = _system.IsBlueprint ? Editor.StandIn(_system) : null;

            _using.IsVisible = shown != null;
            if (shown == null)
                return;

            _usingLead.Text = texts.SystemUsing;

            Blocksets(texts);
            _blocksets.Text = SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, shown);
            _blocksets.Tip = CodexTip.For(_blocksets.Tip, texts.Codex.SystemUsingAbout);
        }

        /// <summary>The list of the blocksets made from the blueprint, made again only where what they are called has changed.</summary>
        void Blocksets(Texts texts)
        {
            var instances = _system.Instances;
            var same = _blocksets != null && _called.Count == instances.Count;
            for (var i = 0; same && i < instances.Count; i++)
                same = _called[i] == SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, instances[i]);

            if (same)
                return;

            _called.Clear();
            for (var i = 0; i < instances.Count; i++)
                _called.Add(SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, instances[i]));

            _blocksets = new ListField(_called.ToArray());
            _blocksets.Edited += Chose;
            _usingChoice.Children.Clear();
            _usingChoice.Children.Add(_blocksets);
        }

        /// <summary>Shows the blocks of the blockset the list was left on.</summary>
        void Chose()
        {
            var at = _called.IndexOf(_blocksets.Text);
            if (at < 0 || at >= _system.Instances.Count)
                return;

            var instance = _system.Instances[at];
            var choose = Editor.Act(CommandKind.ChooseInstance, () => Editor.Choose(_system, instance));
            if (choose != null)
                choose.Execute();
        }

        StatusRow Own(Sequence sequence)
        {
            return new StatusRow
            {
                Mark = Symbols.Sequence,
                Opens = Editor.Act(CommandKind.OpenSequence, () => Editor.Push(new SequencePage(Editor, sequence))),
                Runs = Editor.Act(CommandKind.RunNow, () => Editor.RunNow(sequence)),
                Stops = Editor.Act(CommandKind.StopNow, () => Editor.StopNow(sequence)),
                AboutArguments = new Arguments()
            };
        }

        void Live(StatusRow row, Sequence sequence, int tick)
        {
            var running = SequenceSummary.Live(Editor.Runner, sequence);

            var status = SequenceSummary.Status(sequence, running);
            row.Status = status == SequenceStatus.Off ? "" : SequenceText.Status(Editor.Texts, status);
            row.Running = running != null && running.IsRunning;
            row.JustRan = running != null && running.LastFiredTick >= 0 && tick - running.LastFiredTick < JustRanTicks;
            row.LastRun = Editor.Ago.Said(SequenceSummary.TicksSinceRun(running, tick));
        }

        static void Mark(Texts texts, StatusRow row, SequenceFault fault, Sequence sequence)
        {
            var starting = IsStarting(fault);

            row.Problem = starting ? "" : SequenceText.Problem(texts, fault, sequence);
            row.Level = SequenceText.Level(fault);
            row.Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
        }

        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
        }

        /// <summary>Whether <paramref name="fault" />, about what starts a sequence, is about <paramref name="trigger" />.</summary>
        static bool Concerns(Trigger trigger, SequenceFault fault)
        {
            var moment = trigger as MomentTrigger;
            return fault.Problem != SequenceProblem.EmptySlot || moment != null && moment.Sources.Contains(fault.Which);
        }

        SequenceFault Fault(Sequence sequence, SystemInstance through)
        {
            return SequenceSummary.Fault(Editor.Runner.Get(sequence, through));
        }

        void Collect()
        {
            _wiring = Editor.Program.Wiring;

            _sequences.Clear();
            var sequences = _system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _sequences.Add(sequences[i]);

            _triggers.Clear();
            var triggers = _system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                _triggers.Add(triggers[i]);
        }
    }
}
