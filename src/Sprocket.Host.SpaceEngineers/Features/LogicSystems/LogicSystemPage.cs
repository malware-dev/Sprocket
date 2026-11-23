using Sprocket.Host.SpaceEngineers.Features.Callouts;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

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
        KeyedChildren<object, Element> _sequenceRows;
        readonly List<object> _sequenceKeys = new List<object>();
        CategoryWork _sequenceCategories;

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

        /// <summary>The panel values a trigger starts on or gives are being given in, or null while none is open.</summary>
        public ValuesQuestion Giving => _triggersWork?.Giving;

        /// <summary>The panel a state's starting value or what it holds now is being given in, or null while none is open.</summary>
        public ValuesQuestion Stating => _stateWork?.Asking;

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

        /// <summary>What the Share menu offers of the system: sharing it through the library, and copying it as it is on screen.</summary>
        public override ShareOffer Shares => Editor.Holdings.Offering(_system);

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

        /// <summary>The panel a setting is made, opened or given a value in on the installation panel tab while one is open, or null.</summary>
        public ValuesQuestion Asking => _panelWork?.Asking;

        /// <summary>The rows of the Sequences tab, in order: each sequence's, and each category's heading and end.</summary>
        public Element.Collection SequenceRows => _sequenceList.Children;

        /// <summary>The form of the panel a category of sequences is being named and set in, or null while none is open.</summary>
        public FieldSet CategorySettling => _sequenceCategories.Settling?.Form;

        /// <summary>The rows of the Triggers tab, in order: each trigger's line, and each category's heading and end.</summary>
        public Element.Collection TriggerRows => _triggerList.Children;

        /// <summary>Writes the description being typed, and asks whether to save what was changed here, on the way out.</summary>
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

        /// <inheritdoc />
        public override Element CalloutAnchor(CalloutPlace place)
        {
            return place == CalloutPlace.SlotAdd && _slotPanel != null ? _slotPanel.AddSlot : null;
        }

        /// <summary>Keeps the rows by trigger and by sequence, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _triggersWork = new TriggersTab(Editor, _system, _triggerList, Changes, Locked);
            _sequenceRows = new KeyedChildren<object, Element>(_sequenceList.Children, SequenceRow);
            _sequenceCategories = new CategoryWork(Editor,
                new Arranger(Editor, _system, owner => owner.SequenceList, ChangeKind.MoveSequence, ChangeKind.MoveSequenceCategory), Changes, true);
            _sequenceList.FnAllowedDropIndexes = index => Arranger.Landings(_sequenceKeys, index);
            _sequenceList.FnCanIndent = (from, to) => Arranger.Nests(_sequenceKeys, from, to);
            _sequenceList.FnIsIndented = index => Arranger.StandsIn(_sequenceKeys, index);
            _sequenceList.Reordered += moved =>
            {
                var move = Editor.Act(CommandKind.MoveSequence, () => _sequenceCategories.Arranger.Move(_sequenceKeys, moved.From, moved.To, moved.IsIndented));
                if (move != null)
                    move.Execute();
            };
            _stateWork = new StateTab(Editor, _system, _statePanel, Changes, Locked);
            _panelWork = new SlotPanelTab(Editor, _system, _slotPanel, Changes, Locked);

            int shown;
            if (Editor.Habits.Tabs.TryGetValue(_system.Id, out shown))
                _tabs.SelectedIndex = shown;

            _tabs.SelectionChanged += at =>
            {
                Editor.Habits.Tabs[_system.Id] = at;

                // The plus is on a tab, and shows only while that tab is chosen.
                Editor.Callouts.Reconsider();
            };


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
                    row.Problem = IsStarting(fault) && Concerns(_triggers[i], fault)
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
                var row = (StatusRow)_sequenceRows[sequence];

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
            _stateWork.Hang();

            _sequenceCategories.Hang();
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
            _addTrigger.Show(_triggersWork.Adds(texts, null));
            _newTriggerCategory.Text = StatusRow.Plus + texts.CategoryAdd;
            _newTriggerCategory.Command = Changes(CommandKind.AddCategory, _triggersWork.AddCategory);
            _newTriggerCategory.Tip = CodexTip.For(_newTriggerCategory.Tip, texts.Codex.TermCategory);

            _runs.Clear();
            foreach (var trigger in _triggers)
            {
                _runs.Add(_system.FindSequence(trigger.Runs));
            }

            _triggersWork.Arrange(texts, Editable, Locked());

            // A tab says it is empty only where nothing can be added to it; where something can, the ways to add it say enough.
            _noTriggersLine.IsVisible = _triggers.Count == 0 && !Editable;
            _noTriggers.Text = texts.SystemNoTriggers;

            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;
            _newSequence.Command = Changes(CommandKind.NewSequence, () => Editor.NewSequence(_system));
            _newSequence.Tip = KeptTip.For(_newSequence.Tip, _newSequence.Command != null ? null : Locked());

            // A category just added opens its name for typing once its row stands.
            _newSequenceCategory.Text = StatusRow.Plus + texts.CategoryAdd;
            _newSequenceCategory.Command = Changes(CommandKind.AddCategory, _sequenceCategories.Add);
            _newSequenceCategory.Tip = CodexTip.For(_newSequenceCategory.Tip, texts.Codex.TermCategory);

            var editable = Editable;
            var locked = Locked();
            CategoryList.Listed(_system.SequenceList, _sequenceKeys, true, editable);
            _sequenceRows.Show(_sequenceKeys);
            _noSequences.IsVisible = _sequences.Count == 0 && !editable;
            _noSequences.Text = texts.SystemNoSequences;
            for (var i = 0; i < _sequenceKeys.Count; i++)
            {
                var heading = _sequenceKeys[i] as CategoryHeading;
                if (heading != null)
                    _sequenceCategories.Heading(texts, (CategoryRow)_sequenceRows[heading], heading, Arranger.Carries(_sequenceKeys, i), editable, locked,
                        texts.Codex.CategoryDragSequences, texts.Codex.CategoryRemoveSequences);

                var end = _sequenceKeys[i] as CategoryEnd;
                if (end != null)
                    ArrangeEnd(texts, (CategoryEndRow)_sequenceRows[end], end, i);
            }

            foreach (var sequence in _sequences)
            {
                var row = (StatusRow)_sequenceRows[sequence];
                row.Arranged = true;
                row.Indented = sequence.Category.Length > 0;
                row.Grips(editable, editable ? texts.Codex.SequenceDrag : locked);
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

        /// <summary>The action keeping this logic system to the block the editor was opened from, or taking it back, or null where somebody else holds the construct or no block found opened the editor.</summary>
        public ChoiceEntry KeepHere
        {
            get
            {
                if (Editor.Holder != null || Editor.OpenedFrom == 0)
                    return null;

                var kept = !string.IsNullOrEmpty(_system.ListedOn);
                var here = Editor.World?.FindEntity(Editor.OpenedFrom);
                if (!kept && here == null)
                    return null;

                var text = kept ? Editor.Texts.SystemsListEverywhere : Editor.Texts.SystemsKeepHere;
                var command = Editor.Act(CommandKind.Expand, () => Editor.Do(_system, new ListSystemOn(_system.ListedOn, kept ? "" : Editor.Program.Declare(Editor.World, here).Name)));
                return new ChoiceEntry(text, false, command) { About = Editor.Texts.SystemsKeepHereAbout };
            }
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

        /// <summary>The row for <paramref name="key" /> on the Sequences tab: a sequence's, or a category's heading.</summary>
        Element SequenceRow(object key)
        {
            var sequence = key as Sequence;
            if (sequence != null)
                return Own(sequence);

            return key is CategoryEnd ? (Element)new CategoryEndRow() : new CategoryRow();
        }

        /// <summary>Shows the end <paramref name="end" /> of a category, at <paramref name="at" /> of the rows, as the way to add a sequence into the category.</summary>
        void ArrangeEnd(Texts texts, CategoryEndRow row, CategoryEnd end, int at)
        {
            row.Category = end.Name;
            var adds = Changes(CommandKind.NewSequence, () => Editor.NewSequence(_system, made => _sequenceCategories.Arranger.Placed(_sequenceKeys, at, made)));
            row.Adds.Show(new AddOption(texts.SystemNewSequence, null, new[] { new AddOption(texts.SystemNewSequence, null, adds) }));
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
            row.RunLocked = SequenceText.RunLocked(Editor.Texts, sequence, Editor.StandIn(sequence.System));
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
