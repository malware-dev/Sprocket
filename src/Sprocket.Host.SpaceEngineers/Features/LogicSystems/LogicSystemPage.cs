using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;

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

        int _wiring = -1;

        Keyed<Trigger, TriggerRow> _triggerRows;
        Keyed<Sequence, StatusRow> _sequenceRows;

        /// <summary>The page of <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public LogicSystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        /// <inheritdoc />
        public override string Title => SystemSummary.Describe(Editor.Texts, Editor.Runner.Bindings, _system);

        /// <summary>What the author said this thing is.</summary>
        /// <remarks>
        /// A label to whoever built it, and the only explanation anybody who receives it will ever get. Empty until
        /// somebody writes one, and empty reads as nothing rather than as a blank waiting to be filled.
        /// </remarks>
        public override string Subtitle => _system.Annotation;

        /// <inheritdoc />
        public override string CustomName => _system.Name ?? "";

        /// <summary>A logic system is renamed on its heading, the same act as renaming a sequence on its own.</summary>
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SystemNameAbout;

        /// <summary>A logic system has to be called something.</summary>
        /// <remarks>
        /// Unlike a sequence, which reads as its own sentence when it is unnamed. A logic system is a name and the ones
        /// of it and nothing else, so an empty one leaves a row on the home page that cannot be told apart from any
        /// other.
        /// </remarks>
        public override bool NameRequired => true;

        /// <summary>A switch for a logic system, and none for a blueprint, which does not run: its logic systems do.</summary>
        public override bool CanSwitch => !_system.IsBlueprint;

        /// <summary>The switch of the one of the system being edited.</summary>
        public override bool SwitchIsOn
        {
            get
            {
                var shown = Editor.StandIn(_system);
                return shown != null && shown.Enabled;
            }
        }

        /// <summary>What starts this system's sequences is the system's to walk back.</summary>
        /// <remarks>
        /// Making a trigger, changing one, pointing it somewhere else, deleting one. A sequence's own arrows are for
        /// what a sequence is, and they are on its sentence rather than up here.
        /// </remarks>
        public override bool HasHistory => true;

        /// <inheritdoc />
        public override bool CanUndo => Editor.CanUndo(_system);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system);

        /// <inheritdoc />
        public override bool CanRedo => Editor.CanRedo(_system);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system);

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
            var shown = Editor.StandIn(_system);
            if (shown == null)
                return;

            shown.Enabled = on;
            Editor.Invalidate();
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

        /// <summary>Gives the ways to add something their presses, keeps the rows by trigger and by sequence, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _when.Command = Editor.Act(Cmd.StartWhen, delegate { Editor.StartSequence(_system, new WhenTrigger("")); });
            _every.Command = Editor.Act(Cmd.StartEvery, delegate { Editor.StartSequence(_system, new EveryTrigger(Cadences.Default)); });

            // The way to make a sequence without starting from what would start it. Somebody who knows what the
            // machine should do can write that first and give it a trigger afterwards.
            _newSequence.Command = Editor.Act(Cmd.NewSequence, delegate { Editor.NewSequence(_system); });

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

            for (var i = 0; i < _triggers.Count; i++)
            {
                var row = _triggerRows[_triggers[i]];
                var state = _runs[i] == null ? null : Editor.Runner.Get(_runs[i]);
                row.IsLiveNow = state != null && state.LiveNow(_triggers[i]);

                if (recount)
                {
                    var fault = _runs[i] == null ? SequenceFault.None : Fault(_runs[i], through);
                    row.Problem = IsStarting(fault) ? SequenceText.Problem(fault) : "";
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
                    row.Summary = SequenceText.Doing(Editor.Runner.Bindings, sequence, through);
                    Mark(row, Fault(sequence, through));
                }

                row.Show(texts);
            }
        }

        void Arrange(Texts texts)
        {
            // Walks the triggers' blocks for their names, which is why it runs only when the page is built.

            ArrangeUsing(texts);

            _triggersHeading.Text = texts.SystemTriggers.ToUpperInvariant();
            _triggersHeading.Style = _system.IsBlueprint ? Styles.NextHeading : Styles.Heading;
            _when.Text = StatusRow.Plus + texts.SystemWhen;
            _every.Text = StatusRow.Plus + texts.SystemEvery;

            _runs.Clear();
            for (var i = 0; i < _triggers.Count; i++)
                _runs.Add(_system.FindSequence(_triggers[i].Runs));

            _triggerRows.Show(_triggers);
            for (var i = 0; i < _triggers.Count; i++)
                Word(_triggerRows[_triggers[i]], _triggers[i]);

            _sequencesHeading.Text = texts.SystemSequences.ToUpperInvariant();
            _newSequence.Text = StatusRow.Plus + texts.SystemNewSequence;

            _sequenceRows.Show(_sequences);
            _noSequences.IsVisible = _sequences.Count == 0;
            _noSequences.Text = texts.SystemNoSequences;

            for (var i = 0; i < _sequences.Count; i++)
            {
                // Said before it is done rather than discovered after: the copy having no trigger is the part
                // somebody would otherwise read as a fault.
                var sequence = _sequences[i];
                _sequenceRows[sequence].DuplicateText = sequence.Trigger != null ? texts.SystemCopy : texts.SystemCopyUnstarted;
            }
        }

        void ArrangeUsing(Texts texts)
        {
            _using.IsVisible = _system.IsBlueprint;
            if (!_system.IsBlueprint)
                return;

            var shown = Editor.StandIn(_system);
            _usingLead.Text = texts.SystemUsing;
            _usingName.Say(shown == null ? "" : SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, shown));

            // Pressed only where there is another to choose; otherwise it reads as a part that cannot be pressed.
            _usingName.Command = _system.Instances.Count < 2 ? null : Editor.Act(Cmd.ChooseInstance, delegate { Another(shown); });
        }

        StatusRow Own(Sequence sequence)
        {
            // Copying is offered whether or not it is running: a copy is switched off and started by nothing, so making
            // one cannot disturb what is running. Running it by hand is offered whatever its switch says, because
            // somebody debugging a sequence parks it so its triggers leave it alone, and that is when running it by
            // hand is worth most. Throwing one away is not editing it: nothing half-written can result, and the
            // question asks first.

            return new StatusRow
            {
                Opens = Editor.Act(Cmd.OpenSequence, delegate { Editor.Push(new SequencePage(Editor, sequence)); }),
                Flips = Editor.Act(Cmd.Enable, delegate
                {
                    // Built again, because what else the row offers moves with the switch.
                    sequence.Enabled = !sequence.Enabled;
                    Editor.Invalidate();
                }),
                Copies = Editor.Act(Cmd.DuplicateSequence, delegate { Editor.Duplicate(sequence); }),
                Runs = Editor.Act(Cmd.RunNow, delegate { Editor.RunNow(sequence); }),
                Stops = Editor.Act(Cmd.StopNow, delegate { Editor.StopNow(sequence); }),
                Deletes = Editor.Act(Cmd.DeleteSequence, delegate { Editor.DeleteSequence(sequence); })
            };
        }

        void Live(StatusRow row, Sequence sequence, int tick)
        {
            // Whichever one of the system is going, and the first otherwise: a list of a system's sequences is about
            // none of its ones in particular, and a row that only read the first would sit still while the port airlock
            // cycled.

            var running = SequenceSummary.Live(Editor.Runner, sequence);

            row.Status = SequenceText.Status(SequenceSummary.Status(sequence, running));
            row.Running = running != null && running.IsRunning;
            row.JustRan = running != null && running.LastFiredTick >= 0 && tick - running.LastFiredTick < JustRanTicks;
            row.LastRun = TimeText.Ago(SequenceSummary.TicksSinceRun(running, tick));
            row.SwitchIsOn = sequence.Enabled;
        }

        static void Mark(StatusRow row, SequenceFault fault)
        {
            // News about what starts a sequence goes on the row of the trigger that starts it, and not on the
            // sequence's: a cleared button is the trigger's trouble and the sequence is fine. The warning that nothing
            // starts it at all is the exception, because there is no such row for it to be said on.

            var starting = IsStarting(fault);

            row.Problem = starting ? "" : SequenceText.Problem(fault);
            row.ProblemIsWarning = fault.IsWarning;
            row.Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
            row.Parked = fault.Problem == SequenceProblem.SwitchedOff;
        }

        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
        }

        SequenceFault Fault(Sequence sequence, SystemInstance through)
        {
            return SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, sequence,
                Editor.Runner.Get(sequence, through), through);
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

            row.Deletes = Editor.Act(Cmd.DeleteTrigger, delegate { Editor.Forget(_system, trigger); });
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
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == moment.EventId)
                    return events[i];
            }

            return null;
        }

        void Doing(TriggerRow row, Trigger trigger)
        {
            // A named sequence is a thing in its own right, so it is said by name after "runs" or "stops", and pressing
            // it opens the picker the trigger was made in. One nobody named is what this trigger does, so its act is
            // written out as the parts a step of a sequence uses, each opening the picker on its own column.

            var sequence = _system.FindSequence(trigger.Runs);
            var act = sequence == null || sequence.HasName ? null : sequence.Only;

            if (act == null)
            {
                row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, true, false);
                row.Does.Add(new SequenceSlot(trigger.Runs, sequence == null || !sequence.HasName ? "" : sequence.Name,
                    Editor.Act(Cmd.TriggerTarget, delegate { Editor.ChangeTrigger(_system, trigger, Opening.Nothing, false); })));

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
            return Editor.Act(Cmd.TriggerTarget, delegate { Editor.ChangeTrigger(_system, trigger, opens, true); });
        }

        ICommand Starts(Trigger trigger, Opening opens)
        {
            return Editor.Act(Cmd.OpenTrigger, delegate { Editor.ChangeTrigger(_system, trigger, opens, false); });
        }

        void Another(SystemInstance shown)
        {
            var page = new ChoicePage(Editor, Editor.Texts.SystemUsing);
            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];
                page.Add(SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, instance), instance == shown, delegate
                {
                    Editor.Choose(_system, instance);
                    Editor.Pop();
                });
            }

            Editor.Push(page);
        }

        void Collect()
        {
            // A sequence nobody named is the one thing some trigger does, said on that trigger's own row: listing it
            // here as well would show it twice, and offer a switch and a copy for something that is not a thing in its
            // own right.

            _wiring = Editor.Program.Wiring;

            _sequences.Clear();
            var sequences = _system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].HasName)
                    _sequences.Add(sequences[i]);
            }

            _triggers.Clear();
            var triggers = _system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                _triggers.Add(triggers[i]);
        }
    }
}
