using System;
using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;

namespace Reactor.Editing
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

        StatusListComponent _list;

        public SystemPage(ReactorEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        public int SystemId => _system.Id;

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
            var wanted = (name ?? "").Trim();

            // Refused here as well as wherever it was typed. A target that forgot to ask would otherwise
            // erase the only thing this system can be called by, and the rule would end up living in every
            // target rather than in the one place that owns it.
            if (wanted.Length == 0)
                return;

            _system.Name = wanted;
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

                _list.Entries.Add(entry);
            }

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

            SequenceSummary.Fill(_list.Entries, _sequences, Editor.Runner.Bindings, Editor.Runner, Editor.CurrentTick,
                Editor.SequenceText, Editor.TimeText);

            for (var i = 0; i < _list.Entries.Count && i < _sequences.Count; i++)
                _list.Entries[i].SwitchIsOn = _sequences[i].Enabled;
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
