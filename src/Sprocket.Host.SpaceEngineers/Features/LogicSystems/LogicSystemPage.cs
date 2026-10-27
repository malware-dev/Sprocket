using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
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
using Sprocket.Host.SpaceEngineers.World;
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
        readonly List<Part> _parts = new List<Part>();
        readonly List<object> _slotKeys = new List<object>();
        readonly List<string> _used = new List<string>();
        readonly List<string> _marks = new List<string>();
        readonly SlotFill _fill;
        readonly SlotArranger _arranger;
        readonly OptimizeQuestion _optimizing;

        int _wiring = -1;
        Element _focused;
        ListField _blocksets;
        StateTab _stateWork;
        string _madeName;
        string _madeAbout;
        bool _madeOn;
        NoticeLevel _standing;

        KeyedChildren<Trigger, TriggerRow> _triggerRows;
        KeyedChildren<Sequence, StatusRow> _sequenceRows;
        KeyedChildren<object, Element> _slotRows;
        LengthQuestion _rate;

        /// <summary>The page of <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public LogicSystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
            _fill = new SlotFill(editor, system);
            _arranger = new SlotArranger(editor, system);
            _optimizing = new OptimizeQuestion(editor);
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

        /// <summary>The panel a timer's rate is being given in, or null while none is open.</summary>
        public DurationPanel Rate => _rate != null && _rate.IsOpen ? _rate.Panel : null;

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
        public Composer About => _composer;

        /// <summary>The blueprint's slots, one row each, with the shown installation's blocks for each.</summary>
        public Element.Collection Slots => _slotList.Children;

        /// <summary>Writes the description being typed, and asks whether to keep what was changed here, on the way out.</summary>
        public override bool Leaving()
        {
            Written(_focused);
            _focused = null;
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
            Settle();
            Editor.Undo(_system, _system);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Settle();
            Editor.Redo(_system, _system);
        }

        /// <summary>Brings the view up to date, words and all.</summary>
        public override void Build()
        {
            Refocused();
            Collect();
            Show(true);
        }

        /// <summary>Brings the view up to what the sequences are doing, and asks for the screen to be built again when the program has changed.</summary>
        /// <remarks>Called every tick the page is showing.</remarks>
        public override void Refresh()
        {
            Refocused();

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
            _triggerRows = new KeyedChildren<Trigger, TriggerRow>(_triggerList.Children, delegate { return new TriggerRow(); });
            _sequenceRows = new KeyedChildren<Sequence, StatusRow>(_sequenceList.Children, Own);
            _slotRows = new KeyedChildren<object, Element>(_slotList.Children, SlotRow);
            _stateWork = new StateTab(Editor, _system, _statePanel, Changes, Locked);
            _slotList.Landings = index => SlotArranger.Landings(_slotKeys, index);
            _slotList.Nests = (from, to) => SlotArranger.Nests(_slotKeys, from, to);
            _slotList.StandsIn = index => SlotArranger.StandsIn(_slotKeys, index);
            _slotList.NestDepth = PartRow.Indent;
            _slotList.Moves = (from, to) =>
            {
                var into = _slotList.DroppedIn;
                var move = Editor.Act(CommandKind.MoveSlot, () => _arranger.Move(_slotKeys, from, to, into));
                if (move != null)
                    move.Execute();
            };

            int shown;
            if (Editor.Habits.Tabs.TryGetValue(_system.Id, out shown))
                _tabs.Current = shown;

            _tabs.Switched += at => Editor.Habits.Tabs[_system.Id] = at;

            Show(true);
        }

        void Show(bool reshaped)
        {
            // Live state is read every tick; faults are said again on the tick the runs change.

            if (_triggerRows == null)
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
                var row = _triggerRows[_triggers[i]];
                var poller = Editor.Runner.Watches.Poller(_triggers[i], through);
                row.IsLiveNow = poller != null && poller.Live;

                if (recount)
                {
                    var fault = _runs[i] == null ? SequenceFault.None : Fault(_runs[i], through);
                    var own = _triggers[i].Acts != null && fault.Exists;
                    row.Problem = own || IsStarting(fault) && Concerns(_triggers[i], fault)
                        ? SequenceText.Problem(texts, fault, _runs[i])
                        : "";
                    row.Level = fault.IsWarning ? NoticeLevel.Warning : NoticeLevel.Problem;

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

            // A handful of rows, each still unless one is carried or has just landed.
            _slotList.Slide();
            _stateWork.Slide();
            if (_tabs.Current >= 0 && _tabs.Current < _tabs.Tabs.Count && _tabs.Tabs[_tabs.Current] == _stateTab)
                _stateWork.Live();

            if (_rate != null)
                _rate.Hang();
        }

        /// <summary>Opens the panel the rate of <paramref name="every" /> is given in, which changes it as it closes.</summary>
        void OpenRate(EveryTrigger every)
        {
            if (_rate != null)
                _rate.Finish(false);

            _rate = new LengthQuestion(Editor, every.Interval, true, () => RateChip(every),
                (interval, cancelled) =>
                {
                    if (!cancelled && interval.HasValue && interval.Value != every.Interval && _system.IndexOfTrigger(every) >= 0)
                        Editor.Do(_system, new SetCadence(every, every.Interval, interval.Value));
                });
            _rate.Hang();
        }

        /// <summary>The chip of <paramref name="every" />'s rate, or null where its row does not show one.</summary>
        Element RateChip(EveryTrigger every)
        {
            var row = _triggerRows?[every];
            if (row == null)
                return null;

            foreach (var start in row.Starts)
            {
                if (start is DurationSlot)
                    return row.ChipOf(start);
            }

            return null;
        }

        void Arrange(Texts texts)
        {
            // Walks the triggers' blocks for their names, so it runs only when the page is built.

            ArrangeDescription(texts);
            ArrangeUsing(texts);
            ArrangeLock();

            _triggersTab.Header = texts.SystemTriggers;
            _sequencesTab.Header = texts.SystemSequences;
            _stateTab.Header = texts.SystemState;
            _panelTab.Header = texts.SystemInstallationPanel;
            _triggersTab.Tip = Tip.For(_triggersTab.Tip, texts.Codex.TermTrigger);
            _sequencesTab.Tip = Tip.For(_sequencesTab.Tip, texts.Codex.TermSequence);
            _stateTab.Tip = Tip.For(_stateTab.Tip, texts.Codex.TermLogicSystemStates);
            _panelTab.Tip = Tip.For(_panelTab.Tip, texts.Codex.TermInstallationPanel);
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

            _triggerRows.Show(_triggers);

            // A tab says it is empty only where nothing can be added to it; where something can, the ways to add it say enough.
            _noTriggersLine.IsVisible = _triggers.Count == 0 && !Editable;
            _noTriggers.Text = texts.SystemNoTriggers;
            foreach (var trigger in _triggers)
                Word(_triggerRows[trigger], trigger);

            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;
            _newSequence.Command = Changes(CommandKind.NewSequence, () => Editor.NewSequence(_system));
            _newSequence.Tip = Tip.For(_newSequence.Tip, _newSequence.Command != null ? null : Locked());

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

            ArrangeSlots(texts);

            _noPanelLine.IsVisible = !_describing.IsVisible && _parts.Count == 0 && !Editable;
            _noPanel.Text = texts.SystemNoPanel;
        }

        /// <summary>
        /// Shows <paramref name="mark" /> in the color of <paramref name="level" />, saying in <paramref name="texts" /> where to look, and
        /// lays <paramref name="tab" /> on that level's wash and stripes; or hides the mark and leaves the tab plain where nothing on
        /// it is more than a note.
        /// </summary>
        static void Flag(Tab tab, Glyph mark, NoticeLevel level, Texts texts)
        {
            tab.Style = level == NoticeLevel.Problem ? Styles.TabBroken : level == NoticeLevel.Warning ? Styles.TabWarned : Styles.Tab;
            mark.IsVisible = level != NoticeLevel.Info;
            mark.Base.Tint = level == NoticeLevel.Warning ? Palette.Notice : Palette.Problem;
            mark.Tip = Tip.For(mark.Tip, texts.Codex.SystemTabFlagged);
        }

        /// <summary>The graver of <paramref name="one" /> and <paramref name="other" />: a problem over a warning over a note, as the levels are ordered.</summary>
        static NoticeLevel Worse(NoticeLevel one, NoticeLevel other)
        {
            return other > one ? other : one;
        }

        /// <summary>Shows a blueprint's description, in its box while it may be changed, leaving alone a box being typed in.</summary>
        void ArrangeDescription(Texts texts)
        {
            var blueprint = _system.IsBlueprint;
            var editable = Editable;
            var said = _system.Annotation;

            _describing.IsVisible = blueprint && (editable || said.Length > 0);
            if (!_describing.IsVisible)
                return;

            _descriptionHeading.IsVisible = editable;
            _descriptionHeading.Text = texts.BlueprintDescription.ToUpperInvariant();
            _composer.IsVisible = editable;
            _composer.Show(texts);
            _composer.Guides = Editor.Guides;
            _composer.Hint = texts.BlueprintDescriptionHint;
            if (!_composer.Box.IsFocused)
                _composer.Text = said;

            _description.IsVisible = !editable;
            _description.Markdown = said;
        }

        /// <summary>Lists a blueprint's slots, each with the blocks the shown installation fills it with.</summary>
        void ArrangeSlots(Texts texts)
        {
            var shown = _system.IsBlueprint ? Editor.StandIn(_system) : null;

            _parts.Clear();
            _slotKeys.Clear();
            _system.Acted(_used);
            if (shown != null)
                PartList.Listed(_system, _parts, _slotKeys, true);

            _slotList.IsVisible = _parts.Count > 0;
            _addCategory.IsVisible = _parts.Count > 0;
            _newCategory.IsVisible = Editable;
            _newCategory.Text = StatusRow.Plus + texts.CategoryAdd;
            _newCategory.Command = Changes(CommandKind.AddCategory, () => _arranger.Add(Editor.Texts));
            _newCategory.Tip = Tip.For(_newCategory.Tip, texts.Codex.TermCategory);
            _optimize.Text = texts.SlotOptimize;
            _optimize.Command = Changes(CommandKind.OptimizeSlots, () => _optimizing.Ask(_system));
            _optimize.Tip = Tip.For(_optimize.Tip, Editable ? texts.Codex.SlotOptimizeAbout : Locked());
            _slotRows.Show(_slotKeys);
            if (shown == null)
                return;

            for (var i = 0; i < _slotKeys.Count; i++)
            {
                var heading = _slotKeys[i] as CategoryHeading;
                if (heading != null)
                    ArrangeCategory(texts, (CategoryRow)_slotRows[heading], heading, SlotArranger.Carries(_slotKeys, i));
            }

            foreach (var part in _parts)
            {
                var row = (PartRow)_slotRows[part];
                row.Arranged = true;
                row.Indented = part.Category.Length > 0;
                row.Grips(Editable, Editable ? texts.Codex.SlotDrag : Locked());

                row.Name = part.Id;
                row.Short = part.IsSignal ? part.Short : "";
                row.Mark = MarkText.OfSlot(part);
                row.Described = part.Annotation;
                row.Opens = Changes(CommandKind.OpenSlot, () => Editor.Push(new SlotPage(Editor, _system, part)));
                row.OpensAbout = Editable ? texts.Codex.SlotOpenAbout : Locked();
                row.FillsLocked = Locked();
                row.Fills = Changes(CommandKind.FillPart, () => _fill.Ask(part, shown));

                // A slot nothing uses asks nothing of any installation, and is offered its bin.
                var unused = !_used.Contains(part.Id);
                row.Marks = unused ? null : Marked(texts, part, shown);
                row.Take(Editor.Runner.Bindings, shown.FilledWith(part.Id), part.IsSignal ? shown.Positions(part.Id) : null);
                row.Problem = unused ? texts.SlotUnused : null;
                row.Level = NoticeLevel.Warning;
                row.Removable = unused;
                row.Removes = unused ? Changes(CommandKind.RemoveSlot, () => Editor.Do(_system, ChangeSlots.Removing(_system, shown, part))) : null;
                row.RemovesAbout = Editable ? texts.Codex.SlotRemoveAbout : Locked();

                if (row.Blocks.Count > 0)
                    row.Show(Editor.World);
                else
                    row.Say(SentenceText.Empty, !part.Optional && !unused);
            }
        }

        /// <summary>A row for <paramref name="key" /> of the installation panel: a slot's, or a category's.</summary>
        Element SlotRow(object key)
        {
            if (key is Part)
                return new PartRow(Editor.Texts);

            return new CategoryRow();
        }

        /// <summary>Shows the category <paramref name="heading" /> heads over the <paramref name="carries" /> slots under it, renamed, removed and carried while the system may be changed.</summary>
        void ArrangeCategory(Texts texts, CategoryRow row, CategoryHeading heading, int carries)
        {
            var editable = Editable;
            var name = heading.Name;

            row.Name = name;
            row.Carries = carries;
            row.Arranged = true;
            row.Grips(editable, editable ? texts.Codex.CategoryDrag : Locked());
            row.Renames = Changes(CommandKind.RenameCategory, () => Editor.Rename(row.Plate, new Naming
            {
                Name = name,
                NameRequired = true,
                Accepts = SlotArranger.Accepts,
                Renames = wanted => _arranger.Rename(heading, wanted)
            }));
            row.RenamesAbout = editable ? texts.Codex.CategoryRenameAbout : Locked();
            row.Removes = Changes(CommandKind.RemoveCategory, () => _arranger.Remove(heading));
            row.RemovesAbout = editable ? texts.Codex.CategoryRemove : Locked();
        }

        /// <summary>What the installations other than <paramref name="shown" /> still have to do about <paramref name="part" />, or null where nothing.</summary>
        string Marked(Texts texts, Part part, SystemInstance shown)
        {
            _marks.Clear();
            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] == shown)
                    continue;

                var mark = SlotMarks.Of(Editor.Runner.Bindings, instances[i], part);
                if (mark == SlotMark.None)
                    continue;

                var said = mark == SlotMark.Changed ? texts.SlotMarkChanged : mark == SlotMark.Lost ? texts.SlotMarkLost : texts.SlotMarkNew;
                _marks.Add(Template.Fill(said, "installation", SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, instances[i])));
            }

            return _marks.Count == 0 ? null : string.Join("; ", _marks.ToArray());
        }

        /// <summary>Writes the description box into the blueprint once the keyboard has left it.</summary>
        /// <remarks>Called every tick.</remarks>
        void Refocused()
        {
            var now = Editor.Input?.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the focused box into the blueprint and lets the keyboard go.</summary>
        void Settle()
        {
            Written(_focused);
            _focused = null;
            if (Editor.Input != null)
                Editor.Input.Focus(null);
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the blueprint, where it is the description's box.</summary>
        void Written(Element box)
        {
            if (box == null || _composer == null || !Editable || box != _composer.Box)
                return;

            Editor.Do(_system, new AnnotateSystem(_system.Annotation, _composer.Text));
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
            _blocksets.Tip = Tip.For(_blocksets.Tip, texts.Codex.SystemUsingAbout);
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
            row.Level = fault.IsWarning ? NoticeLevel.Warning : NoticeLevel.Problem;
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

        void Word(TriggerRow row, Trigger trigger)
        {
            row.Kind = Kind(trigger);
            row.Starts.Clear();
            row.Does.Clear();

            var moment = trigger as MomentTrigger;
            var watching = moment as WhenTrigger;
            if (watching != null && Vocabulary.Compares(watching, Editor.SlotBlocks.Events(_system, watching.Sources)))
            {
                Compared(row, watching);
            }
            else if (moment != null)
            {
                var all = watching != null && watching.Quantifier != Quantifier.Any;
                var blocks = Editor.Blocks(_system, moment.Sources);
                var slotted = _system.IsBlueprint;
                if (watching != null && slotted)
                    row.Starts.Add(Reading(watching, false));

                row.Starts.Add(new EntitiesSlot(blocks, moment.Sources, all, MarkText.OfSlots(_system, moment.Sources), Starts(trigger, OpeningColumn.Blocks)));
                row.Starts.Add(new EventSlot(Happening(moment), Starts(trigger, OpeningColumn.What), all && (slotted || blocks.Count > 1)));
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                row.Starts.Add(new DurationSlot(every.Interval, Changes(CommandKind.OpenTrigger, () => OpenRate(every))));

            var watch = trigger as StateTrigger;
            if (watch != null)
                Watched(row, watch);

            Doing(row, trigger);

            row.Deletes = Changes(CommandKind.DeleteTrigger, () => Editor.Starts.Forget(_system, trigger));
            row.Copies = Changes(CommandKind.DuplicateTrigger, () => Editor.Starts.CopyTrigger(_system, trigger));
            row.Locked = Locked();
        }

        static TriggerKind Kind(Trigger trigger)
        {
            if (trigger is EveryTrigger)
                return TriggerKind.Every;

            return trigger is WhenTrigger || trigger is StateTrigger ? TriggerKind.When : TriggerKind.Signal;
        }

        /// <summary>Gives <paramref name="row" /> the parts of a when on one of the system's states: the state, how it compares, and with what.</summary>
        void Watched(TriggerRow row, StateTrigger watch)
        {
            var test = watch.Test;
            var opens = Starts(watch, OpeningColumn.Blocks);
            row.Starts.Add(new SystemStateSlot(test.State, opens));
            row.Starts.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation) }, ComparisonQuestion.StateFields(),
                Changes(CommandKind.OpenTrigger, () => RelateWatch(watch)), ArgumentRole.Compared));
            row.Starts.Add(StateValueQuestion.Part(_system, test.State, test.Value, test.Other, opens, ArgumentRole.Checked));
        }

        /// <summary>Offers is and is not in a menu under the part pressed, and changes what <paramref name="watch" /> starts on to the one chosen.</summary>
        void RelateWatch(StateTrigger watch)
        {
            Editor.AskHere("");
            foreach (var related in new[] { Relation.Is, Relation.IsNot })
            {
                var relation = related;
                Editor.Answer(ComparisonQuestion.Said(relation), delegate
                {
                    var was = watch.Test;
                    if (relation == was.Relation)
                        return;

                    var now = was.Copy();
                    now.Relation = relation;
                    Editor.Do(_system, new SetWatch(watch, was, now));
                }, relation == watch.Test.Relation);
            }
        }

        /// <summary>Gives <paramref name="row" /> the parts of a when on a number.</summary>
        void Compared(TriggerRow row, WhenTrigger when)
        {
            var blocks = Editor.Blocks(_system, when.Sources);
            var value = when.Names(Editor.SlotBlocks.Actions(_system, when.Sources));

            // A line naming slots says how it reads them whatever they hold; one naming blocks, only where it names several.
            var slotted = _system.IsBlueprint;
            var many = slotted || blocks.Count > 1;
            var reading = many ? Reading(when, true) : null;
            var front = SentenceText.Leads(when.Quantifier, slotted);
            if (reading != null && front)
                row.Starts.Add(reading);

            row.Starts.Add(new ActionSlot(value, Starts(when, OpeningColumn.What)));
            if (reading != null && !front)
                row.Starts.Add(reading);

            row.Starts.Add(new EntitiesSlot(blocks, when.Sources, when.Quantifier != Quantifier.Any, MarkText.OfSlots(_system, when.Sources),
                Starts(when, OpeningColumn.Blocks)));
            if (value == null)
                return;

            var fields = ComparisonQuestion.Fields(value, Editor.SlotBlocks.Events(_system, when.Sources));
            var plural = when.Quantifier == Quantifier.All && many;
            row.Starts.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(when.Relation) }, fields.GetRange(0, 1),
                Changes(CommandKind.OpenTrigger, () => Relate(when, fields[0], plural)), ArgumentRole.Compared, plural));
            row.Starts.Add(new ArgumentsSlot(new[] { when.Value }, fields.GetRange(1, fields.Count - 1), Starts(when, OpeningColumn.Arguments), ArgumentRole.Checked));
        }

        /// <summary>Offers the comparisons <paramref name="relation" /> takes in a menu under the part pressed, and changes <paramref name="when" /> to the one chosen.</summary>
        void Relate(WhenTrigger when, Field relation, bool plural)
        {
            Editor.AskHere("");
            for (var i = 0; i < relation.Words.Count; i++)
            {
                var related = ComparisonQuestion.Read(Literal.Name(relation.Means[i]));
                var said = plural ? SentenceText.Plural(relation.Words[i]) : relation.Words[i];
                Editor.Answer(said, delegate
                {
                    if (related == when.Relation)
                        return;

                    var now = when.Copy();
                    now.Relation = related;
                    Editor.Do(_system, new ChangeTrigger(when, now));
                }, related == when.Relation);
            }
        }

        /// <summary>The part saying how <paramref name="when" /> reads the blocks in its slots, pressed to choose another way, a total among them where it <paramref name="compares" /> a number.</summary>
        SentenceSlot Reading(WhenTrigger when, bool compares)
        {
            var slotted = _system.IsBlueprint;
            var said = SentenceText.Reading(when.Quantifier, slotted);
            return new ArgumentsSlot(new[] { Literal.Name(said) }, new[] { Field.Among("reading", new[] { said }, new[] { said }) },
                Changes(CommandKind.OpenTrigger, () => Quantify(when, compares, slotted)), ArgumentRole.Read);
        }

        /// <summary>
        /// Offers the ways <paramref name="when" /> can read its blocks in a menu under the part pressed, in the words of a line
        /// naming slots where <paramref name="slotted" />, a total among them where it <paramref name="compares" /> a number, and
        /// changes it to the one chosen.
        /// </summary>
        void Quantify(WhenTrigger when, bool compares, bool slotted)
        {
            Editor.AskHere("");
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                var reading = SentenceText.Readings[i];
                if (!compares && SentenceText.Totals(reading))
                    continue;

                Editor.Answer(SentenceText.Reading(reading, slotted), delegate
                {
                    if (reading == when.Quantifier)
                        return;

                    var now = when.Copy();
                    now.Quantifier = reading;
                    Editor.Do(_system, new ChangeTrigger(when, now));
                }, reading == when.Quantifier);
            }
        }

        EntityEvent Happening(MomentTrigger moment)
        {
            var events = Editor.SlotBlocks.Events(_system, moment.Sources);
            var eventId = Vocabulary.EventOf(Editor.Program, Editor.Runner.Bindings, _system, moment, Editor.StandIn(_system));
            return Vocabulary.Find(events, eventId);
        }

        void Doing(TriggerRow row, Trigger trigger)
        {
            var set = trigger.Sets;
            if (set != null)
            {
                var opens = Changes(CommandKind.TriggerTarget, () => Editor.Starts.ChangeTrigger(_system, trigger, OpeningColumn.Nothing, false));
                row.DoesLead = SentenceText.Then(row.Kind, false, false, true);
                row.Does.Add(new SystemStateSlot(set.State, opens));
                row.Does.Add(StateValueQuestion.Part(_system, set.State, set.Value, set.From, opens, ArgumentRole.NewValue));
                return;
            }

            var act = trigger.Acts;
            if (act == null)
            {
                var sequence = _system.FindSequence(trigger.Runs);
                row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, true, false);
                row.Does.Add(new SequenceSlot(trigger.Runs, sequence == null || !sequence.HasName ? "" : sequence.Name,
                    Changes(CommandKind.TriggerTarget, () => Editor.Starts.ChangeTrigger(_system, trigger, OpeningColumn.Nothing, false))));

                return;
            }

            var chosen = act.Names(Editor.SlotBlocks.Actions(_system, act.Targets));

            var verb = act.Sets ? SentenceText.Switching(chosen, act.Arguments) : "";
            row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, false, act.Sets && verb.Length == 0);
            if (verb.Length != 0)
                row.Does.Add(ArgumentsSlot.Switching(verb, Does(trigger, OpeningColumn.Arguments)));

            row.Does.Add(new ActionSlot(chosen, Does(trigger, OpeningColumn.What)));
            var blocks = Editor.Blocks(_system, act.Targets);
            row.Does.Add(new EntitiesSlot(blocks, act.Targets, true, MarkText.OfSlots(_system, act.Targets), Does(trigger, OpeningColumn.Blocks))
            {
                Screens = Editor.SlotBlocks.Screens(_system, act.Targets, blocks)
            });
            if (verb.Length != 0)
                return;

            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            var given = new ArgumentsSlot(act.Arguments, fields, Does(trigger, OpeningColumn.Arguments), act.Sets ? ArgumentRole.NewValue : ArgumentRole.Options);
            if (!given.Quiet)
                row.Does.Add(given);
        }

        ICommand Does(Trigger trigger, OpeningColumn opens)
        {
            return Changes(CommandKind.TriggerTarget, () => Editor.Starts.ChangeTrigger(_system, trigger, opens, true));
        }

        ICommand Starts(Trigger trigger, OpeningColumn opens)
        {
            return Changes(CommandKind.OpenTrigger, () => Editor.Starts.ChangeTrigger(_system, trigger, opens, false));
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
