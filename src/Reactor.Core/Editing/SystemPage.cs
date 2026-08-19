using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// One machine: what it is, and everything it does.
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

        public int SystemId { get { return _system.Id; } }

        public override string Title
        {
            get { return SystemSummary.Describe(Editor.World, _system); }
        }

        /// <summary>
        /// What the author said this thing is.
        /// </summary>
        /// <remarks>
        /// A label to whoever built it, and the only explanation anybody who receives it will
        /// ever get. Empty until somebody writes one, and empty reads as nothing rather than
        /// as a blank waiting to be filled.
        /// </remarks>
        public override string Subtitle { get { return _system.Description; } }

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
                _list.Entries.Add(new StatusEntry("",
                    Editor.Register(delegate { Editor.Push(new SequencePage(Editor, sequence)); })));
            }

            components.Add(_list);
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

            SequenceSummary.Fill(_list.Entries, _sequences, Editor.World, Editor.Runner, Editor.CurrentTick);
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
