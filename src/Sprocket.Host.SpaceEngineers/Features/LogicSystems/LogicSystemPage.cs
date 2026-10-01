using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Values;
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
    sealed partial class LogicSystemPage : EditorPage
    {
        const int JustRanTicks = 10;

        readonly AutomationSystem _system;
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<Trigger> _triggers = new List<Trigger>();
        readonly List<Sequence> _runs = new List<Sequence>();

        readonly List<string> _called = new List<string>();

        int _wiring = -1;
        int _quiet;
        ListField _blocksets;
        string _madeName;
        string _madeAbout;
        bool _madeOn;

        Keyed<Trigger, TriggerRow> _triggerRows;
        Keyed<Sequence, StatusRow> _sequenceRows;

        /// <summary>The page of <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public LogicSystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;

            // Here rather than when the screen is built, which happens again after every change.
            _quiet = editor.Program.Wiring;
        }

        /// <inheritdoc />
        public override string Title => SystemSummary.Describe(Editor.Texts, Editor.Runner.Bindings, _system);

        /// <inheritdoc />
        public override bool Edits => true;

        /// <summary>What the author said this thing is, for one logic system; a blueprint's description is read on its panel.</summary>
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

        /// <summary>Which button starts each sequence, and which nothing starts, is read from the toolbars.</summary>
        public override bool CanRefresh => true;

        /// <summary>The logic system just made is as it was made: its name, what it says about itself, its switch, and nothing in it.</summary>
        /// <remarks>A sequence made in it and left untouched has gone already, since its page was left first.</remarks>
        public override bool Untouched => _system.Name == _madeName && _system.Annotation == _madeAbout
            && _system.Enabled == _madeOn && _system.Triggers.Count == 0 && _system.Sequences.Count == 0;

        /// <summary>Deletes the system, asking first; a blueprint only once its name is typed, since every logic system made from it goes too.</summary>
        public override ICommand Deletes => Editor.Act(Cmd.DeleteSystem, delegate { Editor.DeleteSystem(_system); });

        /// <inheritdoc />
        public override string DeleteAbout => _system.IsBlueprint ? Editor.Texts.SystemDeleteBlueprint : Editor.Texts.SystemDelete;

        /// <summary>What starts this system's sequences is the system's to walk back.</summary>
        /// <remarks>
        /// Making a trigger, changing one, pointing it somewhere else, deleting one. A sequence's own arrows are for
        /// what a sequence is, and they are on its sentence rather than up here.
        /// </remarks>
        public override bool HasHistory => true;

        /// <summary>Nothing is walked back while the system may run, as nothing else about it changes then.</summary>
        public override bool CanUndo => Editable && Editor.CanUndo(_system);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system);

        /// <inheritdoc />
        public override bool CanRedo => Editable && Editor.CanRedo(_system);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system);

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

            _system.Name = wanted;
        }

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, Naming.Clean(name));
            return other == null || other == _system;
        }

        /// <inheritdoc />
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            if (_system.IsBlueprint)
                parts[0].After = Editor.Texts.SystemBlueprint;
        }

        /// <inheritdoc />
        public override void Switch(bool on)
        {
            Editor.SwitchSystem(_system, on);
        }

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
            if (_system.Enabled || Editor.Program.Wiring == _quiet || !HasSteps())
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
            Editor.Undo(_system);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Editor.Redo(_system);
        }

        /// <summary>Brings the view up to date, words and all.</summary>
        /// <remarks>Called when the editor builds the screen: on arriving at the page, coming back to it, a change to the program, and a change of language.</remarks>
        public override void Build()
        {
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

            Show(true);
        }

        void Show(bool reshaped)
        {
            // What a row is doing, how long ago it ran and whether a trigger's signal is true are read every tick,
            // because none of them costs anything and a badge read on a cadence would lag behind the door it is about.
            // What a sequence is called is a sentence joined out of its blocks and actions, so it is said again on a
            // cadence, with what is wrong.

            if (_triggerRows == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken one is marked from the first frame.
            var recount = SecondPassed() || reshaped;

            if (reshaped)
                Arrange(texts);

            var through = Editor.StandIn(_system);
            var tick = Editor.CurrentTick;

            if (recount)
                Stopped(through);

            for (var i = 0; i < _triggers.Count; i++)
            {
                var row = _triggerRows[_triggers[i]];
                var poller = Editor.Runner.Poller(_triggers[i], through);
                row.IsLiveNow = poller != null && poller.Live;

                // A trigger's own act has no row of its own, so what goes wrong with it is said on the trigger's.
                if (recount)
                {
                    var fault = _runs[i] == null ? SequenceFault.None : Fault(_runs[i], through);
                    var own = _triggers[i].Acts != null && fault.Exists && !fault.IsWarning;
                    row.Problem = own || IsStarting(fault) ? SequenceText.Problem(texts, fault, _runs[i]) : "";
                }

                row.Show(Editor.World, texts);
            }

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];
                var row = _sequenceRows[sequence];

                Live(row, sequence, tick);

                if (recount)
                {
                    row.Summary = SequenceText.Called(texts, sequence);
                    Mark(texts, row, Fault(sequence, through), sequence);
                }

                row.Show(texts);
            }
        }

        void Arrange(Texts texts)
        {
            // Walks the triggers' blocks for their names, which is why it runs only when the page is built.

            ArrangeUsing(texts);
            ArrangeLock();

            _triggersHeading.Text = texts.SystemTriggers.ToUpperInvariant();
            // A blueprint's line of whose blocks the sentences name stands above it; a logic system of its own has nothing there.
            _triggersHeading.Style = _system.IsBlueprint ? Styles.NextHeading : Styles.Heading;
            _when.Text = StatusRow.Plus + texts.SystemWhen;
            _when.Tip = Tip.For(_when.Tip, texts.SystemWhenAbout);
            _when.Command = Changes(Cmd.StartWhen, delegate { Editor.StartSequence(_system, new WhenTrigger("")); });
            _every.Text = StatusRow.Plus + texts.SystemEvery;
            _every.Tip = Tip.For(_every.Tip, texts.SystemEveryAbout);
            _every.Command = Changes(Cmd.StartEvery, delegate { Editor.StartSequence(_system, new EveryTrigger(Cadences.Default)); });

            _runs.Clear();
            for (var i = 0; i < _triggers.Count; i++)
            {
                var trigger = _triggers[i];
                _runs.Add(trigger.Acts != null ? Editor.Runner.Alone(_system, trigger) : _system.FindSequence(trigger.Runs));
            }

            _triggerRows.Show(_triggers);
            for (var i = 0; i < _triggers.Count; i++)
                Word(_triggerRows[_triggers[i]], _triggers[i]);

            _sequencesHeading.Text = texts.SystemSequences.ToUpperInvariant();
            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;
            _newSequence.Tip = Tip.For(_newSequence.Tip, texts.SystemNewSequenceAbout);

            // The way to make a sequence without starting from what would start it. Somebody who knows what the
            // machine should do can write that first and give it a trigger afterwards.
            _newSequence.Command = Changes(Cmd.NewSequence, delegate { Editor.NewSequence(_system); });

            _sequenceRows.Show(_sequences);
            _noSequences.IsVisible = _sequences.Count == 0;
            _noSequences.Text = texts.SystemNoSequences;

            for (var i = 0; i < _sequences.Count; i++)
            {
                // Said before it is done rather than discovered after: the copy having no trigger is the part
                // somebody would otherwise read as a fault.
                var sequence = _sequences[i];
                var row = _sequenceRows[sequence];
                row.DuplicateText = sequence.Trigger != null ? texts.SystemCopy : texts.SystemCopyUnstarted;
                row.About = texts.SystemOpenSequence;
                row.AboutArguments.Set("name", SequenceText.Called(texts, sequence));

                // Deleting one is changing the system, so it waits for the switch like any other change.
                row.Deletes = Changes(Cmd.DeleteSequence, delegate { Editor.DeleteSequence(sequence); });
                row.DeleteText = texts.SequenceDelete;
                row.Locked = Locked();
            }
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
            _halted.Text = halt == null ? "" : SequenceText.Stopped(Editor.Texts, halt, _system);
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

        /// <summary>Says, for a blueprint, whose blocks the sentences name, as a list of the blocksets made from it, with the way to set up its configuration panel.</summary>
        /// <remarks>A logic system of its own names its own blocks, which its heading already says.</remarks>
        void ArrangeUsing(Texts texts)
        {
            var blueprint = _system.IsBlueprint;
            var shown = Editor.StandIn(_system);

            _using.IsVisible = blueprint;
            if (!blueprint)
                return;

            _usingLead.Text = texts.SystemUsing;
            _usingLead.IsVisible = shown != null;
            _usingChoice.IsVisible = shown != null;

            // Quiet rather than gone while the system is on, so it is the same button saying why it cannot be pressed.
            var editable = Editable;
            _editPanel.Text = texts.SystemEditPanel;
            _editPanel.Command = Changes(Cmd.EditBlueprint, delegate { Editor.Push(new BlueprintPage(Editor, _system, true)); });
            _editPanel.IsEnabled = editable;
            _editPanel.Tip = Tip.For(_editPanel.Tip, editable ? texts.SystemEditPanelAbout : Locked());

            if (shown == null)
                return;

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

            _blocksets = new ListField(_called.ToArray(), delegate { return Editor.Windows; });
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
            var choose = Editor.Act(Cmd.ChooseInstance, delegate { Editor.Choose(_system, instance); });
            if (choose != null)
                choose.Execute();
        }

        StatusRow Own(Sequence sequence)
        {
            // Copying is offered whether or not the system is on: a copy is started by nothing, so making one cannot
            // disturb what is running. Running it by hand is offered whatever the switch says, because somebody working
            // on a system switches it off so its triggers leave it alone, and that is when running a sequence by hand is
            // worth most.

            return new StatusRow
            {
                Opens = Editor.Act(Cmd.OpenSequence, delegate { Editor.Push(new SequencePage(Editor, sequence)); }),
                Copies = Editor.Act(Cmd.DuplicateSequence, delegate { Editor.Duplicate(sequence); }),
                Runs = Editor.Act(Cmd.RunNow, delegate { Editor.RunNow(sequence); }),
                Stops = Editor.Act(Cmd.StopNow, delegate { Editor.StopNow(sequence); }),
                AboutArguments = new Arguments()
            };
        }

        void Live(StatusRow row, Sequence sequence, int tick)
        {
            // Whichever one of the system is going, and the first otherwise: a list of a system's sequences is about
            // none of its ones in particular, and a row that only read the first would sit still while the port airlock
            // cycled.

            var running = SequenceSummary.Live(Editor.Runner, sequence);

            row.Status = SequenceText.Status(Editor.Texts, SequenceSummary.Status(sequence, running));
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
            row.ProblemIsWarning = fault.IsWarning;
            row.Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
        }

        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
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
            if (moment != null)
            {
                var when = moment as WhenTrigger;
                row.Starts.Add(new EntitiesSlot(Editor.Blocks(_system, moment.Sources),
                    when != null && when.RequiresAll, Starts(trigger, Opening.Blocks)));

                row.Starts.Add(new EventSlot(Happening(moment), Starts(trigger, Opening.What)));
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                row.Starts.Add(new DurationSlot(every.IntervalTicks, Starts(trigger, Opening.Nothing)));

            Doing(row, trigger);

            row.Deletes = Changes(Cmd.DeleteTrigger, delegate { Editor.Forget(_system, trigger); });
            row.Locked = Locked();
        }

        static TriggerKind Kind(Trigger trigger)
        {
            if (trigger is EveryTrigger)
                return TriggerKind.Every;

            return trigger is WhenTrigger ? TriggerKind.When : TriggerKind.Required;
        }

        EntityEvent Happening(MomentTrigger moment)
        {
            // Asked of the blocks rather than remembered, because what a block can announce is the block's to say, and
            // a moment that is no longer among them reads as an empty part rather than as words that could never fire.

            var events = Vocabulary.EventsFor(Editor.Runner.Bindings, Editor.Seen(_system, moment.Sources));
            var eventId = Vocabulary.EventOf(Editor.Program, Editor.Runner.Bindings, _system, moment, Editor.StandIn(_system));
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == eventId)
                    return events[i];
            }

            return null;
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
                    Changes(Cmd.TriggerTarget, delegate { Editor.ChangeTrigger(_system, trigger, Opening.Nothing, false); })));

                return;
            }

            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_system, act.Targets)));

            row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, false, act.Sets);
            row.Does.Add(new ActionSlot(chosen, Does(trigger, Opening.What)));
            row.Does.Add(new EntitiesSlot(Editor.Blocks(_system, act.Targets), true, Does(trigger, Opening.Blocks)));

            // What it was given, where it takes anything, so the line says all of what it does.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            row.Does.Add(new ValuesSlot(act.Values, fields, Does(trigger, Opening.Values)));
        }

        ICommand Does(Trigger trigger, Opening opens)
        {
            return Changes(Cmd.TriggerTarget, delegate { Editor.ChangeTrigger(_system, trigger, opens, true); });
        }

        ICommand Starts(Trigger trigger, Opening opens)
        {
            return Changes(Cmd.OpenTrigger, delegate { Editor.ChangeTrigger(_system, trigger, opens, false); });
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
