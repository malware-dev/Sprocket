using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.Words;

namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// One system: what it is, and everything it does.
    /// </summary>
    /// <remarks>
    /// Where somebody spends their time. Close to what the editor was before systems existed,
    /// because the work itself has not changed - what changed is that the sequences on this
    /// screen are the ones belonging to the thing you came here about, rather than every
    /// sequence on the ship.
    /// </remarks>
    sealed class SystemPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<Trigger> _triggers = new List<Trigger>();

        /// <summary>The rows being built, before they are published in one go.</summary>
        readonly List<StatusEntry> _gathered = new List<StatusEntry>();

        /// <summary>
        /// What starts things, above what they are.
        /// </summary>
        /// <remarks>
        /// Two lists rather than one, because they are two kinds of thing and somebody builds them
        /// in that order: a sequence is what happens, a trigger is a reason for it to happen. One
        /// list of both would have to say which each row was, on every row.
        /// <para>
        /// Each is under its own row of ways to add to it, which is what tells them apart on screen:
        /// the same shape twice, and what you can add says what the list below it holds.
        /// </para>
        /// </remarks>
        StatusListComponent _starts;

        StatusListComponent _list;


        public SystemPage(ReactorEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        public override string Title => SystemSummary.Describe(Editor.Runner.Bindings, _system);

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

        public override bool CanSwitch => true;

        public override bool SwitchIsOn => _system.Enabled;

        public override void Switch(bool on)
        {
            _system.Enabled = on;
            Editor.Invalidate();
        }

        public override void Build(List<EditorComponent> components)
        {
            Collect();

            // Named, because this screen holds two lists of different things and the rows in them
            // read alike. Reactor's own nouns rather than a phrase talking round them.
            components.Add(new HeadingComponent("Triggers"));

            // The same shape as the ship's own screen: the ways to add something on top, then
            // what is already here. One screen learnt twice - and here it is learnt twice on one
            // screen, which is the other half of what separates the two lists.
            var start = new StartComponent();
            Editor.AddStarters(start, _system);
            components.Add(start);

            _starts = new StatusListComponent();

            // Nothing said while it is empty. A system with no triggers has no sequences either, and
            // the list below already says so - two messages about one emptiness is one too many, and
            // the ways to add something are on the row above both of them.
            _starts.EmptyText = "";

            _gathered.Clear();

            for (var i = 0; i < _triggers.Count; i++)
                _gathered.Add(Starting(i, _triggers[i]));

            _starts.Entries.Reset(_gathered);
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

                _gathered.Add(entry);
            }

            // In one go, for the reason the ship's own list does it: a list that says when it changes
            // should say so once, about a list that is finished.
            _list.Entries.Reset(_gathered);
            components.Add(_list);
        }

        static Action<bool> Switch(Sequence sequence)
        {
            return delegate(bool on) { sequence.Enabled = on; };
        }

        public override void Refresh()
        {
            var before = _sequences.Count;
            Collect();
            if (_sequences.Count != before)
            {
                Editor.Invalidate();
                return;
            }

            SequenceSummary.Fill(_list.Entries, _sequences, Editor.Runner, Editor.CurrentTick);

            for (var i = 0; i < _list.Entries.Count && i < _sequences.Count; i++)
                _list.Entries[i].SwitchIsOn = _sequences[i].Enabled;

            // What a row is doing, and how long ago it ran, are read every tick because neither
            // costs anything. What it is called is a sentence joined out of its blocks and actions,
            // and that changes when somebody edits it or renames a block it names - so it is said
            // again on a cadence rather than sixty times a second.
            if (SecondPassed())
            {
                SequenceSummary.Word(_list.Entries, _sequences, Editor.Runner.Bindings);
                Mark();
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

                entries[i].Problem = starting ? "" : SequenceText.Problem(fault.Problem);
                entries[i].ProblemIsWarning = fault.IsWarning;
                entries[i].Problems = !starting && fault.Exists && !fault.IsWarning ? 1 : 0;
            }

            var starts = _starts.Entries;
            for (var i = 0; i < starts.Count && i < _triggers.Count; i++)
            {
                Sequence runs = _system.FindSequence(_triggers[i].Runs);
                var fault = runs == null ? SequenceFault.None : Fault(runs);
                var mine = IsStarting(fault);

                starts[i].Problem = mine ? SequenceText.Problem(fault.Problem) : "";
                starts[i].Problems = mine ? 1 : 0;
            }
        }

        /// <summary>Whether this fault is about what starts the sequence rather than about a step.</summary>
        static bool IsStarting(SequenceFault fault)
        {
            return fault.Exists && !fault.IsWarning && fault.Step == SequenceFault.Trigger;
        }

        SequenceFault Fault(Sequence sequence)
        {
            return SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, sequence,
                Editor.Runner.Get(sequence));
        }

        /// <summary>
        /// One trigger, as a row: what starts it, and what it then does.
        /// </summary>
        /// <remarks>
        /// Two presses, one for each of the two sentences it reads as: the line that says when it
        /// happens asks that again, and the line that says what it does asks that again. Nothing is
        /// asked about which half was meant, because the words somebody pressed are the answer.
        /// <para>
        /// Its commands are numbered by where it sits among the system's triggers. That is a
        /// position, which an id may never be - but the list is rebuilt whenever the program changes
        /// and nothing else can reorder it, so the position a target is holding is the trigger it was
        /// given.
        /// </para>
        /// </remarks>
        StatusEntry Starting(int which, Trigger trigger)
        {
            var asked = trigger;

            var entry = new StatusEntry(
                SequenceText.Trigger(Editor.Runner.Bindings, trigger),
                Editor.Register(Cmd.OpenTrigger, which,
                    delegate { Editor.ChangeTrigger(_system, asked); }));

            entry.Status = SequenceText.Runs(Editor.Runner.Bindings, _system, trigger);
            entry.StatusCommand = Editor.Register(Cmd.TriggerTarget, which,
                delegate { Editor.ChangeTarget(_system, asked); });

            var going = trigger;
            entry.DeleteCommand = Editor.Register(Cmd.DeleteTrigger, which,
                delegate { Editor.Forget(_system, going); });

            return entry;
        }

        void Collect()
        {
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
