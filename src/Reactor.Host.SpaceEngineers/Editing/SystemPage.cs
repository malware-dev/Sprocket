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

        /// <summary>The rows being built, before they are published in one go.</summary>
        readonly List<StatusEntry> _gathered = new List<StatusEntry>();

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

            // The same shape as the ship's own screen: the ways to add something on top, then
            // what is already here. One screen learnt twice.
            var start = new StartComponent();
            Editor.AddStarters(start, _system);
            components.Add(start);

            _list = new StatusListComponent();
            _list.Fills = true;
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
        /// Marks the sequences that are broken, and says what is wrong with each.
        /// </summary>
        /// <remarks>
        /// The row somebody arrives at from the ship's screen and the row they open to mend it are
        /// the same row, which is the whole point of the mark being here: the trail is a mark on
        /// every screen between the front door and the line at fault, and a screen in the middle
        /// that stayed quiet would end it.
        /// <para>
        /// One apiece rather than a count. A sequence is one thing that either works or does not,
        /// and <see cref="StatusEntry.Problem" /> already says which way.
        /// </para>
        /// </remarks>
        void Mark()
        {
            var entries = _list.Entries;
            for (var i = 0; i < entries.Count && i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];
                var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, sequence,
                    Editor.Runner.Get(sequence));

                entries[i].Problem = SequenceText.Problem(fault.Problem);
                entries[i].ProblemIsWarning = fault.IsWarning;
                entries[i].Problems = fault.Exists && !fault.IsWarning ? 1 : 0;
            }
        }

        void Collect()
        {
            _sequences.Clear();
            var sequences = _system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _sequences.Add(sequences[i]);
        }
    }
}
