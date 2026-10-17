using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
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
    /// <remarks>
    /// Where somebody spends their time. The elements it is drawn with are declared in
    /// <c>LogicSystemPage.View.cs</c>, and nothing else is; this half is what it knows, what it says and what it
    /// does.
    /// </remarks>
    public sealed partial class LogicSystemPage : EditorPage
    {
        const int JustRanTicks = 10;

        readonly AutomationSystem _system;
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<Trigger> _triggers = new List<Trigger>();
        readonly List<Sequence> _runs = new List<Sequence>();

        readonly List<string> _called = new List<string>();
        readonly List<Part> _parts = new List<Part>();
        readonly List<string> _used = new List<string>();
        readonly List<string> _marks = new List<string>();
        readonly SlotFill _fill;

        int _wiring = -1;
        int _quiet;
        Element _focused;
        ListField _blocksets;
        string _madeName;
        string _madeAbout;
        bool _madeOn;

        Keyed<Trigger, TriggerRow> _triggerRows;
        Keyed<Sequence, StatusRow> _sequenceRows;
        Keyed<Part, PartRow> _slotRows;
        LengthQuestion _rate;

        /// <summary>The page of <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public LogicSystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
            _fill = new SlotFill(editor, system);

            // Here rather than when the screen is built, which happens again after every change.
            _quiet = editor.Program.Wiring;
        }

        /// <inheritdoc />
        public override string Title => SystemSummary.Describe(Editor.Texts, Editor.Runner.Bindings, _system);

        /// <inheritdoc />
        /// <remarks>Led by the logic system's mark, as every heading is led by the mark of what it names.</remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].Mark = Symbols.LogicSystem;
            parts[0].MarkAbout = Editor.Texts.HeadingLogicSystem;
        }

        /// <inheritdoc />
        public override bool Edits => true;

        /// <summary>The panel a timer's rate is being given in, or null while none is open.</summary>
        public DurationPanel Rate => _rate != null && _rate.IsOpen ? _rate.Panel : null;

        /// <summary>What the author said this thing is, for one logic system; a blueprint's is written at the top of the page.</summary>
        /// <remarks>
        /// A label to whoever built it, and the only explanation anybody who receives it will ever get. Empty until
        /// somebody writes one, and empty reads as nothing rather than as a blank waiting to be filled. A blueprint's is
        /// rich text of any length, which the heading's one line has no room for.
        /// </remarks>
        public override string Subtitle => _system.IsBlueprint ? "" : _system.Annotation;

        /// <inheritdoc />
        public override string CustomName => _system.Name ?? "";

        /// <summary>A logic system is renamed on its heading, the same act as renaming a sequence on its own.</summary>
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SystemNameAbout;

        /// <summary>A logic system has to be called something.</summary>
        /// <remarks>
        /// Unlike a sequence, which is called by its number when it is unnamed. A logic system is a name and the ones
        /// of it and nothing else, so an empty one leaves a row on the home page that cannot be told apart from any
        /// other.
        /// </remarks>
        public override bool NameRequired => true;

        /// <summary>The system's switch, a blueprint's included, which is also what keeps it from being changed while it may run.</summary>
        public override bool CanSwitch => true;

        /// <inheritdoc />
        public override bool SwitchIsOn => _system.Enabled;

        /// <inheritdoc />
        public override string SwitchAbout => Editor.SwitchAbout(_system);

        /// <inheritdoc />
        public override string Help => Terms.LogicSystem;

        /// <summary>The logic system just made is as it was made: its name, what it says about itself, its switch, and nothing in it.</summary>
        /// <remarks>A sequence made in it and left untouched has gone already, since its page was left first.</remarks>
        public override bool Untouched => _system.Name == _madeName && _system.Annotation == _madeAbout
            && _system.Enabled == _madeOn && _system.Triggers.Count == 0 && _system.Sequences.Count == 0;

        /// <summary>Deletes the system, asking first; a blueprint only once its name is typed, since every logic system made from it goes too.</summary>
        public override ICommand Deletes => Editor.Act(Cmd.DeleteSystem, () => Editor.DeleteSystem(_system));

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.SystemDelete;

        /// <summary>What starts this system's sequences is the system's to walk back.</summary>
        /// <remarks>
        /// Making a trigger, changing one, pointing it somewhere else, deleting one. A sequence's own arrows are for
        /// what a sequence is, and they are on its sentence rather than up here.
        /// </remarks>
        public override bool HasHistory => true;

        /// <summary>A step is walked back under the rule it was made under, which each step says for itself.</summary>
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
            var wanted = Naming.Clean(name);

            // Refused here as well as wherever it was typed, so the rule lives in the one place that owns it. A name
            // another system already has is refused for the same reason: it is how a person tells two apart.
            if (wanted.Length == 0 || !AcceptsName(wanted))
                return;

            Editor.Do(_system, new RenameSystem(_system.Name, wanted));
        }

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, Naming.Clean(name));
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

        /// <summary>Offers to switch a changed system on, on the way out.</summary>
        /// <remarks>
        /// A system is switched off while it is changed, so a half-written rule never fires, and the cost is somebody
        /// finishing, walking away and concluding it does not work. Leaving it is the moment that costs one press to put
        /// right, and moving between its sequences and triggers is not leaving it. Only when something was changed, only
        /// once per change, and never for a system with nothing in it to run. Dismissing the question is not answering it,
        /// so pressing back asks again.
        /// </remarks>
        public override bool Leaving()
        {
            Written(_focused);
            _focused = null;

            // Nor for a version waiting for a choice.
            if (_system.Enabled || Editor.Program.Wiring == _quiet || !HasSteps() || Editor.Versions.Rival(_system) != null)
                return true;

            var arguments = new Arguments();
            arguments.Set("name", _system.Name);
            Editor.Ask(Editor.Texts.SystemSwitchOnQuestion, arguments);

            Editor.Answer(Editor.Texts.SystemSwitchItOn, delegate
            {
                _quiet = Editor.Program.Wiring;
                Editor.SwitchSystem(_system, true);
                Editor.Pop();
            });

            // The one Enter takes: leaving it off is what pressing back already meant, and the key that gets pressed
            // without reading must not switch something on.
            Editor.Answer(Editor.Texts.SystemLeaveItOff, delegate
            {
                _quiet = Editor.Program.Wiring;
                Editor.Pop();
            }, true);

            return false;
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
        /// <remarks>Called when the editor builds the screen: on arriving at the page, coming back to it, a change to the program, and a change of language.</remarks>
        public override void Build()
        {
            // A box just left is written first: the description filled below would otherwise put back what it replaced.
            Refocused();
            Collect();
            Show(true);
        }

        /// <summary>Brings the view up to what the sequences are doing, and asks for the screen to be built again when the program has changed.</summary>
        /// <remarks>
        /// Every tick the page is showing. Whether the program changed is one comparison, since everything that
        /// moves it bumps <see cref="Automation.Wiring" />, including a host that has taken a block out of a trigger.
        /// </remarks>
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
            _triggerRows = new Keyed<Trigger, TriggerRow>(_triggerList.Children, delegate { return new TriggerRow(); });
            _sequenceRows = new Keyed<Sequence, StatusRow>(_sequenceList.Children, Own);
            _slotRows = new Keyed<Part, PartRow>(_slotList.Children, delegate { return new PartRow(); });

            Show(true);
        }

        void Show(bool reshaped)
        {
            // What a row is doing, how long ago it ran and whether a trigger's signal is true are read every tick,
            // because none of them costs anything and a badge read on a cadence would lag behind the door it is about.
            // What a sequence is called changes only by an edit, which builds the rows again. What is wrong is said again on
            // the tick the runs change.

            if (_triggerRows == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken one is marked from the first frame.
            var recount = Revised() || reshaped;

            if (reshaped)
                Arrange(texts);

            var through = Editor.StandIn(_system);
            var tick = Editor.CurrentTick;

            if (recount)
                Stopped(through);

            for (var i = 0; i < _triggers.Count; i++)
            {
                var row = _triggerRows[_triggers[i]];
                var poller = Editor.Runner.Watches.Poller(_triggers[i], through);
                row.IsLiveNow = poller != null && poller.Live;

                // A trigger's own act has no row of its own, so what goes wrong with it is said on the trigger's.
                if (recount)
                {
                    var fault = _runs[i] == null ? SequenceFault.None : Fault(_runs[i], through);
                    var own = _triggers[i].Acts != null && fault.Exists && !fault.IsWarning;
                    row.Problem = own || IsStarting(fault) && Concerns(_triggers[i], fault)
                        ? SequenceText.Problem(texts, fault, _runs[i])
                        : "";
                    row.Level = NoticeLevel.Problem;

                    // A button trigger's copy says why its start is empty until it is picked, in place of the empty slot that follows.
                    if (Editor.Starts.Copied(_triggers[i]))
                    {
                        row.Problem = texts.TriggerCopiedButton;
                        row.Level = NoticeLevel.Info;
                    }
                }

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

                row.Show(texts);
            }

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
            // Walks the triggers' blocks for their names, which is why it runs only when the page is built.

            ArrangeDescription(texts);
            ArrangeUsing(texts);
            ArrangeLock();

            _triggersHeading.Text = texts.SystemTriggers.ToUpperInvariant();
            // A blueprint's line of whose blocks the sentences name stands above it; a logic system of its own has nothing there.
            _triggersHeading.Style = _system.IsBlueprint ? Styles.NextHeading : Styles.Heading;
            _addTrigger.Show(new Adder(texts.SystemTrigger, texts.SystemTriggerAbout, new[]
            {
                new Adder(SentenceText.WhenWord, texts.SystemWhenAbout,
                    Changes(Cmd.StartWhen, () => Editor.Starts.StartSequence(_system, new WhenTrigger("")))),
                new Adder(SentenceText.EveryWord, texts.SystemEveryAbout,
                    Changes(Cmd.StartEvery, () => Editor.Starts.StartSequence(_system, new EveryTrigger(Editor.NewLength))))
            }));

            _runs.Clear();
            foreach (var trigger in _triggers)
            {
                _runs.Add(trigger.Acts != null ? Editor.Runner.Alone(_system, trigger) : _system.FindSequence(trigger.Runs));
            }

            _triggerRows.Show(_triggers);
            foreach (var trigger in _triggers)
                Word(_triggerRows[trigger], trigger);

            _sequencesHeading.Text = texts.SystemSequences.ToUpperInvariant();
            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;
            _newSequence.Tip = Tip.For(_newSequence.Tip, texts.SystemNewSequenceAbout);

            // The way to make a sequence without starting from what would start it. Somebody who knows what the
            // machine should do can write that first and give it a trigger afterwards.
            _newSequence.Command = Changes(Cmd.NewSequence, () => Editor.NewSequence(_system));

            _sequenceRows.Show(_sequences);
            _noSequences.IsVisible = _sequences.Count == 0;
            _noSequences.Text = texts.SystemNoSequences;

            foreach (var sequence in _sequences)
            {
                // Said before it is done rather than discovered after: the copy having no trigger is the part
                // somebody would otherwise read as a fault.
                var row = _sequenceRows[sequence];
                row.DuplicateText = sequence.Trigger != null ? texts.SystemCopy : texts.SystemCopyUnstarted;
                row.About = texts.SystemOpenSequence;
                row.AboutArguments.Set("name", SequenceText.Called(texts, sequence));

                // Copying one or deleting it is changing the system, so it waits for the switch like any other change.
                row.Copies = Changes(Cmd.DuplicateSequence, () => Editor.Duplicate(sequence));
                row.Deletes = Changes(Cmd.DeleteSequence, () => Editor.DeleteSequence(sequence));
                row.DeleteText = texts.SequenceDelete;
                row.Locked = Locked();
            }

            ArrangeSlots(texts);

            _panelHeading.Text = texts.SystemInstallationPanel.ToUpperInvariant();
            _panelHeading.IsVisible = _describing.IsVisible || _slotsHeading.IsVisible;
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

            // Only over the box: the description read as it is needs no heading.
            _descriptionHeading.IsVisible = editable;
            _descriptionHeading.Text = texts.BlueprintDescription.ToUpperInvariant();
            _composer.IsVisible = editable;
            _composer.Show(texts);
            _composer.Guides = Editor.Act(Cmd.OpenCodex, delegate
            {
                var term = Editor.Codex.Find(Terms.Writing);
                if (term != null)
                    Editor.OpenCodex(term);
            });
            _composer.Hint = texts.BlueprintDescriptionHint;
            if (!_composer.Box.IsFocused)
                _composer.Text = said;

            _description.IsVisible = !editable;
            _description.Markdown = said;
        }

        /// <summary>
        /// Lists a blueprint's slots, each with the blocks the installation shown fills it with and what the others still have
        /// to do about it; a press on its name opens it, and a press on its blocks chooses others.
        /// </summary>
        void ArrangeSlots(Texts texts)
        {
            var shown = _system.IsBlueprint ? Editor.StandIn(_system) : null;

            _parts.Clear();
            _system.Acted(_used);
            if (shown != null)
                PartList.Listed(_system, _used, _parts);

            _slotsHeading.IsVisible = _parts.Count > 0;
            _slotList.IsVisible = _parts.Count > 0;
            _slotsHeading.Text = texts.BlueprintParts.ToUpperInvariant();
            _slotRows.Show(_parts);
            if (shown == null)
                return;

            foreach (var part in _parts)
            {
                var row = _slotRows[part];

                row.Name = part.Id;
                row.Mark = part.IsSignal ? Symbols.Signal : Symbols.Block;
                row.Described = part.Annotation;
                // Like everything else on this screen, waiting for the switch, and saying why.
                row.Opens = Changes(Cmd.OpenSlot, () => Editor.Push(new SlotPage(Editor, _system, part)));
                row.OpensAbout = Editable ? texts.SlotOpenAbout : Locked();
                row.Fills = Changes(Cmd.FillPart, () => _fill.Ask(part, shown));
                row.Marks = Marked(texts, part, shown);
                row.Take(Editor.Runner.Bindings, shown.FilledWith(part.Id), part.IsSignal ? shown.Positions(part.Id) : null);

                if (row.Blocks.Count > 0)
                    row.Show(Editor.World);
                else
                    row.Say(SentenceText.Empty, !part.Optional);
            }
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

                var mark = Marks.Of(Editor.Runner.Bindings, _system, instances[i], part, _used);
                if (mark == SlotMark.None)
                    continue;

                var said = mark == SlotMark.Changed ? texts.SlotMarkChanged : mark == SlotMark.Lost ? texts.SlotMarkLost : texts.SlotMarkNew;
                _marks.Add(Template.Fill(said, "installation", SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, instances[i])));
            }

            return _marks.Count == 0 ? null : string.Join("; ", _marks.ToArray());
        }

        /// <summary>Writes the description box into the blueprint once the keyboard has left it, read every tick because it is one comparison.</summary>
        void Refocused()
        {
            var now = Editor.Input?.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the box that holds the keyboard into the blueprint and lets the keyboard go, so what comes next starts from what was typed.</summary>
        /// <remarks>Before a step back, so it takes back what was typed and the box then shows what the step leaves.</remarks>
        void Settle()
        {
            Written(_focused);
            _focused = null;
            if (Editor.Input != null)
                Editor.Input.Focus(null);
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the blueprint, where it is the description's box.</summary>
        /// <remarks>Only once the keyboard has left it, so a description being typed is one step back rather than one a letter.</remarks>
        void Written(Element box)
        {
            if (box == null || _composer == null || box != _composer.Box || !Editable)
                return;

            Editor.Do(_system, new Annotate(_system.Annotation, _composer.Text));
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
                    : Editor.Texts.SystemTurnOffToEdit;
        }

        /// <summary>A command made only while the system may be changed, and null while it may not.</summary>
        ICommand Changes(Cmd kind, Action action)
        {
            return Editable ? Editor.Act(kind, action) : null;
        }

        /// <summary>Whether any sequence in the system has anything to do.</summary>
        bool HasSteps()
        {
            var sequences = _system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].Steps.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Says, for a blueprint, whose blocks the sentences name, as a list of the installations made from it.</summary>
        /// <remarks>A logic system of its own names its own blocks, which its heading already says.</remarks>
        void ArrangeUsing(Texts texts)
        {
            var shown = _system.IsBlueprint ? Editor.StandIn(_system) : null;

            _using.IsVisible = shown != null;
            if (shown == null)
                return;

            _usingLead.Text = texts.SystemUsing;

            Blocksets(texts);
            _blocksets.Text = SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, shown);
            _blocksets.Tip = Tip.For(_blocksets.Tip, texts.SystemUsingAbout);
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
            var choose = Editor.Act(Cmd.ChooseInstance, () => Editor.Choose(_system, instance));
            if (choose != null)
                choose.Execute();
        }

        StatusRow Own(Sequence sequence)
        {
            // Running it by hand is offered whatever the switch says, because somebody working on a system switches it off so
            // its triggers leave it alone, and that is when running a sequence by hand is worth most.

            return new StatusRow
            {
                Mark = Symbols.Sequence,
                Opens = Editor.Act(Cmd.OpenSequence, () => Editor.Push(new SequencePage(Editor, sequence))),
                Runs = Editor.Act(Cmd.RunNow, () => Editor.RunNow(sequence)),
                Stops = Editor.Act(Cmd.StopNow, () => Editor.StopNow(sequence)),
                AboutArguments = new Arguments()
            };
        }

        void Live(StatusRow row, Sequence sequence, int tick)
        {
            // Whichever one of the system is going, and the first otherwise: a list of a system's sequences is about
            // none of its ones in particular, and a row that only read the first would sit still while the port airlock
            // cycled.

            var running = SequenceSummary.Live(Editor.Runner, sequence);

            // Nothing while the system is off: its switch says so.
            var status = SequenceSummary.Status(sequence, running);
            row.Status = status == SequenceStatus.Off ? "" : SequenceText.Status(Editor.Texts, status);
            row.Running = running != null && running.IsRunning;
            row.JustRan = running != null && running.LastFiredTick >= 0 && tick - running.LastFiredTick < JustRanTicks;
            row.LastRun = Editor.Ago.Said(SequenceSummary.TicksSinceRun(running, tick));
        }

        static void Mark(Texts texts, StatusRow row, SequenceFault fault, Sequence sequence)
        {
            // News about what starts a sequence goes on the row of the trigger that starts it, and not on the
            // sequence's: a cleared button is the trigger's trouble and the sequence is fine. The warning that nothing
            // starts it at all is the exception, because there is no such row for it to be said on.

            var starting = IsStarting(fault);

            row.Problem = starting ? "" : SequenceText.Problem(texts, fault, sequence);
            row.Level = fault.IsWarning ? NoticeLevel.Warning : NoticeLevel.Problem;
            row.Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
        }

        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
        }

        /// <summary>Whether <paramref name="fault" />, about what starts a sequence, is about <paramref name="trigger" />: an empty slot is about the triggers that start from it, and anything else about every trigger of the sequence.</summary>
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
            // Whether or not it names anything. A trigger the host has taken a block out of - somebody cleared that
            // button in the game - would otherwise be a row with nothing on it to press, saying it cannot fire and
            // offering no way to say what should start it instead.

            row.Kind = Kind(trigger);
            row.Starts.Clear();
            row.Does.Clear();

            var moment = trigger as MomentTrigger;
            var watching = moment as WhenTrigger;
            var seen = moment == null ? null : Editor.Seen(_system, moment.Sources);
            if (watching != null && Vocabulary.Compares(Editor.Runner.Bindings, watching, seen))
            {
                Compared(row, watching, seen);
            }
            else if (moment != null)
            {
                var all = watching != null && watching.Quantifier != Quantifier.Any;
                var blocks = Editor.Blocks(_system, moment.Sources);
                row.Starts.Add(new EntitiesSlot(blocks, all, Starts(trigger, Opening.Blocks)));
                row.Starts.Add(new EventSlot(Happening(moment), Starts(trigger, Opening.What), all && blocks.Count > 1));
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                row.Starts.Add(new DurationSlot(every.Interval, Changes(Cmd.OpenTrigger, () => OpenRate(every))));

            Doing(row, trigger);

            row.Deletes = Changes(Cmd.DeleteTrigger, () => Editor.Starts.Forget(_system, trigger));
            row.Copies = Changes(Cmd.DuplicateTrigger, () => Editor.Starts.CopyTrigger(_system, trigger));
            row.Locked = Locked();
        }

        static TriggerKind Kind(Trigger trigger)
        {
            if (trigger is EveryTrigger)
                return TriggerKind.Every;

            return trigger is WhenTrigger ? TriggerKind.When : TriggerKind.Signal;
        }

        /// <summary>
        /// Gives <paramref name="row" /> the parts of a when on a number, read as a check reads one: how several blocks are
        /// read, the value on its blocks, how it compares and with what, the first and third each chosen from a menu under it.
        /// </summary>
        void Compared(TriggerRow row, WhenTrigger when, IList<string> seen)
        {
            var bindings = Editor.Runner.Bindings;
            var blocks = Editor.Blocks(_system, when.Sources);
            var value = when.Names(Vocabulary.ActionsFor(bindings, seen));
            if (blocks.Count > 1)
            {
                var said = SentenceText.Quantified(when.Quantifier);
                row.Starts.Add(new ArgumentsSlot(new[] { Literal.Name(said) }, new[] { Field.Among("reading", new[] { said }, new[] { said }) },
                    Changes(Cmd.OpenTrigger, () => Quantify(when)), Given.Read));
            }

            row.Starts.Add(new ActionSlot(value, Starts(when, Opening.What)));
            row.Starts.Add(new EntitiesSlot(blocks, when.Quantifier != Quantifier.Any, Starts(when, Opening.Blocks)));
            if (value == null)
                return;

            var fields = Comparing.Fields(value, Vocabulary.EventsFor(bindings, seen));
            var plural = when.Quantifier == Quantifier.All && blocks.Count > 1;
            row.Starts.Add(new ArgumentsSlot(new[] { Comparing.Of(when.Relation) }, fields.GetRange(0, 1),
                Changes(Cmd.OpenTrigger, () => Relate(when, fields[0], plural)), Given.Compared, plural));
            row.Starts.Add(new ArgumentsSlot(new[] { when.Value }, fields.GetRange(1, fields.Count - 1), Starts(when, Opening.Arguments), Given.Checked));
        }

        /// <summary>Offers the comparisons <paramref name="relation" /> takes in a menu under the part pressed, the one <paramref name="when" /> makes marked, and changes it to the one chosen as one edit.</summary>
        void Relate(WhenTrigger when, Field relation, bool plural)
        {
            Editor.AskHere("");
            for (var i = 0; i < relation.Words.Count; i++)
            {
                var related = Comparing.Read(Literal.Name(relation.Means[i]));
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

        /// <summary>Offers the ways <paramref name="when" /> can read its blocks in a menu under the part pressed, in the sentence's own words, and changes it to the one chosen as one edit.</summary>
        void Quantify(WhenTrigger when)
        {
            Editor.AskHere("");
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                var reading = SentenceText.Readings[i];
                Editor.Answer(SentenceText.Quantified(reading), delegate
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
            // Asked of the blocks rather than remembered, because what a block can announce is the block's to say, and
            // a moment that is no longer among them reads as an empty part rather than as words that could never fire.

            var events = Vocabulary.EventsFor(Editor.Runner.Bindings, Editor.Seen(_system, moment.Sources));
            var eventId = Vocabulary.EventOf(Editor.Program, Editor.Runner.Bindings, _system, moment, Editor.StandIn(_system));
            return Vocabulary.Find(events, eventId);
        }

        void Doing(TriggerRow row, Trigger trigger)
        {
            // A sequence is a thing in its own right, so it is said by name after "runs" or "stops", and pressing it
            // opens the picker the trigger was made in. An act of the trigger's own is written out as the parts a step
            // of a sequence uses, each opening the picker on its own column.

            var act = trigger.Acts;
            if (act == null)
            {
                var sequence = _system.FindSequence(trigger.Runs);
                row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, true, false);
                row.Does.Add(new SequenceSlot(trigger.Runs, sequence == null || !sequence.HasName ? "" : sequence.Name,
                    Changes(Cmd.TriggerTarget, () => Editor.Starts.ChangeTrigger(_system, trigger, Opening.Nothing, false))));

                return;
            }

            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_system, act.Targets)));

            // A state set on or off reads as enabled or disabled, as on a step.
            var verb = act.Sets ? SentenceText.Switching(chosen, act.Arguments) : "";
            row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, false, act.Sets && verb.Length == 0);
            if (verb.Length != 0)
                row.Does.Add(ArgumentsSlot.Switching(verb, Does(trigger, Opening.Arguments)));

            row.Does.Add(new ActionSlot(chosen, Does(trigger, Opening.What)));
            row.Does.Add(new EntitiesSlot(Editor.Blocks(_system, act.Targets), true, Does(trigger, Opening.Blocks)));
            if (verb.Length != 0)
                return;

            // What it was given, where it takes anything, so the line says all of what it does.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            var given = new ArgumentsSlot(act.Arguments, fields, Does(trigger, Opening.Arguments), act.Sets ? Given.NewValue : Given.Options);
            if (!given.Quiet)
                row.Does.Add(given);
        }

        ICommand Does(Trigger trigger, Opening opens)
        {
            return Changes(Cmd.TriggerTarget, () => Editor.Starts.ChangeTrigger(_system, trigger, opens, true));
        }

        ICommand Starts(Trigger trigger, Opening opens)
        {
            return Changes(Cmd.OpenTrigger, () => Editor.Starts.ChangeTrigger(_system, trigger, opens, false));
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
