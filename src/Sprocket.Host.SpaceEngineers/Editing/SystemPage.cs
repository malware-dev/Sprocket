using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blueprints;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// One system: what it is, and everything it does.
    /// </summary>
    /// <remarks>
    /// Where somebody spends their time. Close to what the editor was before systems existed,
    /// because the work itself has not changed - what changed is that the sequences on this
    /// screen are the ones belonging to the thing somebody came here about, rather than every
    /// sequence on the ship.
    /// </remarks>
    sealed class SystemPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<Trigger> _triggers = new List<Trigger>();

        readonly List<StatusEntry> _gathered = new List<StatusEntry>();

        readonly List<SentenceTrigger> _sentences = new List<SentenceTrigger>();

        int _wiring = -1;

        readonly List<Sequence> _runs = new List<Sequence>();

        TriggerListComponent _starts;

        StatusListComponent _list;


        public SystemPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        public override string Title => SystemSummary.Describe(Editor.Texts, Editor.Runner.Bindings, _system);

        /// <summary>
        /// What the author said this thing is.
        /// </summary>
        /// <remarks>
        /// A label to whoever built it, and the only explanation anybody who receives it will
        /// ever get. Empty until somebody writes one, and empty reads as nothing rather than
        /// as a blank waiting to be filled.
        /// </remarks>
        public override string Subtitle => _system.Annotation;

        /// <summary>
        /// The whole system, running or parked.
        /// </summary>
        /// <remarks>
        /// Separate from each sequence's own switch rather than a shortcut for setting them
        /// all, so parking an airlock to work on it and starting it again afterwards leaves a
        /// sequence somebody had deliberately parked still parked.
        /// </remarks>
        /// <summary>
        /// A system is renamed the same way a sequence is: on its heading.
        /// </summary>
        /// <remarks>
        /// It could only be named when it was made, which meant a typo was permanent and
        /// somebody who thought better of "Airlock" a week later had no way to say so. There
        /// is no reason the two should differ - it is the same act on the same kind of screen.
        /// </remarks>
        public override string CustomName => _system.Name ?? "";

        public override bool CanRename => true;

        public override string RenamePrompt => "What is it?";

        public override string NameAbout => "This logic system's name.\nPress to rename it.";

        /// <summary>
        /// A system has to be called something.
        /// </summary>
        /// <remarks>
        /// Unlike a sequence, which reads as its own sentence when it is unnamed. A system is a name and
        /// the ones of it and nothing else, so an empty one leaves a row on the front door that cannot be
        /// told apart from any other.
        /// </remarks>
        public override bool NameRequired => true;

        public override void Rename(string name)
        {
            var wanted = Naming.Clean(name);

            // Refused here as well as wherever it was typed. A target that forgot to ask would otherwise
            // erase the only thing this system can be called by, and the rule would end up living in every
            // target rather than in the one place that owns it. A name another system already has is
            // refused for the same reason: it is how a person tells two systems apart.
            if (wanted.Length == 0 || !AcceptsName(wanted))
                return;

            _system.Name = wanted;
        }

        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, Naming.Clean(name));
            return other == null || other == _system;
        }

        /// <summary>A switch for a logic system, and none for a blueprint, which does not run: its logic systems do.</summary>
        public override bool CanSwitch => !_system.IsBlueprint;

        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            if (_system.IsBlueprint)
                parts[0].After = "blueprint";
        }

        /// <summary>The switch of the one of the system being edited.</summary>
        public override bool SwitchIsOn
        {
            get
            {
                SystemInstance shown = Editor.StandIn(_system);
                return shown != null && shown.Enabled;
            }
        }

        public override void Switch(bool on)
        {
            SystemInstance shown = Editor.StandIn(_system);
            if (shown == null)
                return;

            shown.Enabled = on;
            Editor.Invalidate();
        }

        public override void Build(List<EditorComponent> components)
        {
            Collect();

            // A blueprint is edited through one of its logic systems, so whose blocks every sentence below
            // names is said before any of them - see UsingComponent.
            if (_system.IsBlueprint)
                components.Add(Using());

            // Named, because this screen holds two lists of different things and the rows in them
            // read alike. Sprocket's own nouns rather than a phrase talking round them.
            var triggers = new HeadingComponent("Triggers");
            triggers.Follows = _system.IsBlueprint;
            components.Add(triggers);

            // The same shape as the ship's own screen: the ways to add something on top, then
            // what is already here. One screen learnt twice - and here it is learnt twice on one
            // screen, which is the other half of what separates the two lists.
            var start = new StartComponent();
            Editor.AddStarters(start, _system);
            components.Add(start);

            // Nothing said while it is empty. A system with no triggers has no sequences either,
            // and the list below already says so - two messages about one emptiness is one too many,
            // and the ways to add something are on the row above both of them.
            _starts = new TriggerListComponent();

            _sentences.Clear();
            _runs.Clear();

            for (var i = 0; i < _triggers.Count; i++)
            {
                _sentences.Add(Starting(i, _triggers[i]));
                _runs.Add(_system.FindSequence(_triggers[i].Runs));
            }

            _starts.Triggers.Reset(_sentences);
            components.Add(_starts);

            var sequences = new HeadingComponent("Sequences");
            sequences.Follows = true;
            components.Add(sequences);

            // The way to make a sequence without starting from what would start it. Somebody who
            // knows what the machine should do can write that first and give it a trigger
            // afterwards.
            var making = new StartComponent();
            making.Add("New sequence", Editor.Register(Cmd.NewSequence,
                delegate { Editor.NewSequence(_system); }));

            components.Add(making);

            // Nothing claims the height, so the screen scrolls as a whole - headings, the ways to
            // add, and both lists moving together. Handing it to the sequences would pin everything
            // above them, and a system with a dozen triggers would push its sequences off the bottom
            // with no way to reach them.
            _list = new StatusListComponent();
            _list.EmptyText = "No sequences yet.";

            _gathered.Clear();

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];
                var entry = new StatusEntry("",
                    Editor.Register(Cmd.OpenSequence, sequence.Id,
                        delegate { Editor.Push(new SequencePage(Editor, sequence)); }));

                // The same switch the sentence carries, at the level somebody is looking at.
                // A system has one on the ship's list and a sequence has one on its own screen;
                // the row in between having none was the odd one out, and parking one rule
                // while leaving the rest running is an ordinary thing to want.
                entry.SwitchCommand = Editor.RegisterToggle(Cmd.Enable, sequence.Id,
                    Switch(sequence));
                entry.SwitchIsOn = sequence.Enabled;

                // Offered whether or not this one is running, unlike everything on the sequence's own
                // screen: a copy is switched off and started by nothing, so making one cannot disturb
                // what is running, and a button that came and went with a switch would be one more
                // rule to hold.
                var copied = sequence;
                entry.DuplicateCommand = Editor.Register(Cmd.DuplicateSequence, sequence.Id,
                    delegate { Editor.Duplicate(copied); });

                // Said before it is done rather than discovered after: the copy having no trigger is
                // the part somebody would otherwise read as a fault.
                entry.DuplicateText = sequence.Trigger != null
                    ? "Creates a copy of this sequence, switched off and with no trigger of its own"
                    : "Creates a copy of this sequence, switched off";

                // Whatever the switch beside it says. Somebody debugging a sequence parks it so
                // its triggers leave it alone, and that is when running it by hand is worth most.
                var run = sequence;
                entry.RunCommand = Editor.Register(Cmd.RunNow, sequence.Id,
                    delegate { Editor.RunNow(run); });

                entry.StopCommand = Editor.Register(Cmd.StopNow, sequence.Id,
                    delegate { Editor.StopNow(run); });

                // Whether or not it is running, unlike everything that rewires one. Throwing a
                // sequence away is not editing it: nothing half-written can result, the question
                // asks first, and a machine somebody wants rid of while it is running is exactly
                // when they want rid of it.
                var going = sequence;
                entry.DeleteCommand = Editor.Register(Cmd.DeleteSequence, sequence.Id,
                    delegate { Editor.DeleteSequence(going); });

                _gathered.Add(entry);
            }

            // In one go, for the reason the ship's own list does it: a list that says when it changes
            // should say so once, about a list that is finished.
            _list.Entries.Reset(_gathered);
            components.Add(_list);
        }

        /// <summary>
        /// Parking a sequence from its row, which changes what else the row offers.
        /// </summary>
        /// <remarks>
        /// The rebuild is the point as much as the switch is: a sequence that may be running is not
        /// rewired, so the bin comes and goes with it - and a command decided when the screen was
        /// built has to be decided again when what it depends on moves.
        /// </remarks>
        Action<bool> Switch(Sequence sequence)
        {
            return delegate(bool on)
            {
                sequence.Enabled = on;
                Editor.Invalidate();
            };
        }

        public override void Refresh()
        {
            // Asked rather than worked out. Everything that moves the program bumps this, including
            // a host that has taken a block out of a trigger, so one comparison answers what walking
            // every sequence and every trigger would - and this runs sixty times a second.
            if (_wiring != Editor.Program.Wiring)
            {
                Editor.Invalidate();
                return;
            }

            Live();

            SequenceSummary.Fill(_list.Entries, _sequences, Editor.Runner, Editor.CurrentTick);

            for (var i = 0; i < _list.Entries.Count && i < _sequences.Count; i++)
                _list.Entries[i].SwitchIsOn = _sequences[i].Enabled;

            // What a row is doing, and how long ago it ran, are read every tick because neither
            // costs anything. What it is called is a sentence joined out of its blocks and actions,
            // and that changes when somebody edits it or renames a block it names - so it is said
            // again on a cadence rather than sixty times a second.
            if (SecondPassed())
            {
                SequenceSummary.Word(_list.Entries, _sequences, Editor.Runner.Bindings, Editor.StandIn(_system));
                Mark();
            }
        }

        /// <summary>
        /// What each trigger's own signal reads, right now.
        /// </summary>
        /// <remarks>
        /// One of the few things on this screen worth a tick. A signal is true for as long as a
        /// door stands open and false the instant it shuts, and a badge read on a cadence would
        /// lag behind the door it is about - which is the one thing it exists to make legible.
        /// <para>
        /// What it costs is a lookup apiece and nothing else. Which sequence a trigger is about is
        /// resolved when the rows are built, because that cannot change without the program
        /// changing, and the program changing rebuilds this screen.
        /// </para>
        /// </remarks>
        void Live()
        {
            var starts = _starts.Triggers;
            for (var i = 0; i < starts.Count && i < _runs.Count; i++)
            {
                SequenceRunner state = _runs[i] == null ? null : Editor.Runner.Get(_runs[i]);
                starts[i].IsLiveNow = state != null && state.LiveNow(_triggers[i]);
            }
        }

        /// <summary>
        /// What starts this system's sequences is the system's to walk back.
        /// </summary>
        /// <remarks>
        /// Making a trigger, changing one, pointing it somewhere else, deleting one. A sequence's own
        /// arrows are for what a sequence is - its steps, its name, what it does - and they are on
        /// its sentence rather than up here.
        /// </remarks>
        public override bool HasHistory => true;

        public override bool CanUndo => Editor.CanUndo(_system);

        public override string UndoText => Editor.UndoLabel(_system);

        public override void Undo()
        {
            Editor.Undo(_system);
        }

        public override bool CanRedo => Editor.CanRedo(_system);

        public override string RedoText => Editor.RedoLabel(_system);

        public override void Redo()
        {
            Editor.Redo(_system);
        }

        /// <summary>
        /// Marks what is broken, and says what is wrong with each.
        /// </summary>
        /// <remarks>
        /// The row somebody arrives at from the ship's screen and the row they open to mend it are
        /// the same row, which is the whole point of the mark being here: the trail is a mark on
        /// every screen between the front door and the thing at fault, and a screen in the middle
        /// that stayed quiet would end it.
        /// <para>
        /// News about what starts a sequence goes on the row of the thing that starts it, and not on
        /// the sequence's - a cleared button is the trigger's trouble and the sequence is fine. The
        /// warning that nothing starts it at all is the exception, because there is no such row for
        /// it to be said on.
        /// </para>
        /// <para>
        /// One apiece rather than a count. A row is one thing that either works or does not, and
        /// <see cref="StatusEntry.Problem" /> already says which way.
        /// </para>
        /// </remarks>
        void Mark()
        {
            var entries = _list.Entries;
            for (var i = 0; i < entries.Count && i < _sequences.Count; i++)
            {
                var fault = Fault(_sequences[i]);
                var starting = IsStarting(fault);

                var parked = fault.Problem == SequenceProblem.SwitchedOff;

                entries[i].Problem = starting ? "" : SequenceText.Problem(fault);
                entries[i].ProblemIsWarning = fault.IsWarning;
                entries[i].Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
                entries[i].Parked = parked;
            }

            var starts = _starts.Triggers;
            for (var i = 0; i < starts.Count && i < _triggers.Count; i++)
            {
                Sequence runs = _system.FindSequence(_triggers[i].Runs);
                var fault = runs == null ? SequenceFault.None : Fault(runs);

                starts[i].Problem = IsStarting(fault) ? SequenceText.Problem(fault) : "";
            }
        }

        /// <summary>Whether this fault is about what starts the sequence rather than about a step.</summary>
        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
        }

        SequenceFault Fault(Sequence sequence)
        {
            SystemInstance through = Editor.StandIn(_system);
            return SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, sequence,
                Editor.Runner.Get(sequence, through), through);
        }

        /// <summary>
        /// One trigger, as the sentence it reads as: when it happens, then what it does.
        /// </summary>
        /// <remarks>
        /// Every part is pressable and opens the question it was answered with - the blocks and the
        /// moment lead to the picker, what it does leads to the list the wizard asks with. Nothing
        /// asks which half was meant, because the part somebody pressed is the answer.
        /// <para>
        /// Its commands are numbered by where it sits among the system's triggers. That is a
        /// position, which an id may never be - but the list is rebuilt whenever the program changes
        /// and nothing else can reorder it, so the position a target is holding is the trigger it was
        /// given.
        /// </para>
        /// </remarks>
        SentenceTrigger Starting(int which, Trigger trigger)
        {
            var asked = trigger;

            var sentence = new SentenceTrigger(Kind(trigger));

            // Whether or not it names anything. A trigger the host has taken a block out of - somebody
            // cleared that button in the game - would otherwise be a row with nothing on it to press,
            // saying it cannot fire and offering no way to say what should start it instead.
            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                var when = moment as WhenTrigger;
                sentence.Slots.Add(new EntitiesSlot(Editor.Blocks(_system, moment.Sources),
                    when != null && when.RequiresAll, Starts(which, 0, asked, Opening.Blocks)));

                sentence.Slots.Add(new EventSlot(Happening(moment), Starts(which, 1, asked, Opening.What)));
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                sentence.Slots.Add(new DurationSlot(every.IntervalTicks, Starts(which, 0, asked, Opening.Nothing)));

            Doing(sentence, which, trigger);

            sentence.DeleteCommand = Editor.Register(Cmd.DeleteTrigger, which,
                delegate { Editor.Forget(_system, asked); });

            return sentence;
        }

        static TriggerKind Kind(Trigger trigger)
        {
            if (trigger is EveryTrigger)
                return TriggerKind.Every;

            return trigger is WhenTrigger ? TriggerKind.When : TriggerKind.Required;
        }

        /// <summary>
        /// The moment this trigger watches for, or null while nothing valid is chosen.
        /// </summary>
        /// <remarks>
        /// Asked of the blocks rather than remembered, because what a block can announce is the
        /// block's to say - and a moment that is no longer among them reads as an empty slot rather
        /// than as words that could never fire.
        /// </remarks>
        EntityEvent Happening(MomentTrigger moment)
        {
            var events = Vocabulary.EventsFor(Editor.Runner.Bindings, Editor.Seen(_system, moment.Sources));
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == moment.EventId)
                    return events[i];
            }

            return null;
        }

        /// <summary>
        /// The second half: the sequence it is about, or the one act it does itself.
        /// </summary>
        /// <remarks>
        /// A named sequence is a thing in its own right, so it is said by name after "runs" or
        /// "stops". One nobody named is what this trigger does, so its act is written out as the
        /// same two slots a step of a sequence uses - one thing done by a trigger and the same thing
        /// done by a step then read alike.
        /// <para>
        /// Pressing a sequence opens the picker the trigger was made in, where what it does sits
        /// beside what starts it. Pressing one act opens the picker that act was chosen in.
        /// </para>
        /// </remarks>
        void Doing(SentenceTrigger sentence, int which, Trigger trigger)
        {
            var target = trigger;

            Sequence sequence = _system.FindSequence(trigger.Runs);
            var act = sequence == null || sequence.HasName ? null : sequence.Only;

            if (act == null)
            {
                var pointed = Editor.Register(Cmd.TriggerTarget, which,
                    delegate { Editor.ChangeTrigger(_system, target, Opening.Nothing, false); });

                sentence.DoesLead = SentenceText.Then(sentence.Kind, trigger.Stops, true, false);
                sentence.Does.Add(new SequenceSlot(trigger.Runs,
                    sequence == null || !sequence.HasName ? "" : sequence.Name, pointed));

                return;
            }

            EntityAction chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_system, act.Targets)));

            // Each part opens the trigger's picker on the page the action was made on, at its column.
            sentence.DoesLead = SentenceText.Then(sentence.Kind, trigger.Stops, false, act.Sets);
            sentence.Does.Add(new ActionSlot(chosen, Does(which, 0, target, Opening.What)));

            sentence.Does.Add(new EntitiesSlot(Editor.Blocks(_system, act.Targets), true,
                Does(which, 1, target, Opening.Blocks)));

            // What it was given, where it takes anything, so the line says all of what it does.
            // Pressing it reopens the whole choice, which opens on these values.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            sentence.Does.Add(new ValuesSlot(act.Values, fields, Does(which, 2, target, Opening.Values)));
        }

        /// <summary>What pressing a part of what a trigger does does: opens its picker on that part's column.</summary>
        int Does(int which, int part, Trigger trigger, Opening opens)
        {
            return Editor.Register(Cmd.TriggerTarget, which, part,
                delegate { Editor.ChangeTrigger(_system, trigger, opens, true); });
        }

        /// <summary>What pressing a part of what starts a trigger does: opens its picker on that part's column.</summary>
        int Starts(int which, int part, Trigger trigger, Opening opens)
        {
            return Editor.Register(Cmd.OpenTrigger, which, part,
                delegate { Editor.ChangeTrigger(_system, trigger, opens, false); });
        }

        /// <summary>Whose blocks the blueprint is being edited through, and the way to choose another.</summary>
        UsingComponent Using()
        {
            SystemInstance shown = Editor.StandIn(_system);

            var line = new UsingComponent();
            line.Lead = "Using the blocks of";
            line.Name = shown == null ? "" : SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, shown);
            line.Command = _system.Instances.Count < 2
                ? 0
                : Editor.Register(Cmd.ChooseInstance, _system.Id, delegate { Another(shown); });
            return line;
        }

        /// <summary>Asks which of the blueprint's logic systems to edit it through instead.</summary>
        void Another(SystemInstance shown)
        {
            var page = new ChoicePage(Editor, "Using the blocks of");
            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                SystemInstance instance = instances[i];
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
            _wiring = Editor.Program.Wiring;

            // Only the ones that stand on their own. A sequence nobody named is the one thing some
            // trigger does, said on that trigger's own row - listing it here as well would show it
            // twice, and offer a switch and a copy for something that is not a thing in its own
            // right.
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
