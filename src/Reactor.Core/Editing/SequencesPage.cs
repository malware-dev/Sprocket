using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// Everything this Reactor is running, one line each.
    /// </summary>
    /// <remarks>
    /// The secondary view, never the front door. The number of sequences grows without
    /// limit while the number of blocks does not, so opening onto a wall of sequences
    /// would be the fastest possible way to lose somebody.
    /// </remarks>
    sealed class SequencesPage : EditorPage
    {
        readonly List<Sequence> _sequences = new List<Sequence>();

        SequenceListComponent _list;

        public SequencesPage(ReactorEditor editor) : base(editor)
        {
        }

        public override string Title { get { return "Your automation"; } }

        /// <summary>
        /// Empty means empty, not "0 sequences". A count of nothing reads like a
        /// report of failure on a screen whose job is to invite someone in.
        /// </summary>
        public override string Subtitle
        {
            get
            {
                if (_sequences.Count == 0)
                    return "";
                return _sequences.Count == 1 ? "1 sequence" : _sequences.Count + " sequences";
            }
        }

        public override void Build(EditorScreen screen)
        {
            Collect();

            // An empty Reactor opens on the question rather than on an apology. The
            // first thing anyone meets is the word that starts the sentence they were
            // already thinking.
            if (_sequences.Count == 0)
            {
                _list = null;
                screen.Components.Add(new HeadingComponent(
                    "Nothing here yet. What should set something off?"));
                screen.Components.Add(Starters());
                return;
            }

            _list = new SequenceListComponent();

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];
                _list.Entries.Add(new SequenceEntry(sequence.Id, "",
                    Editor.Register(delegate { Editor.Push(new SequencePage(Editor, sequence)); })));
            }

            screen.Components.Add(_list);
        }

        ChoiceListComponent Starters()
        {
            var list = new ChoiceListComponent();

            list.Entries.Add(new ChoiceEntry("When something happens", false,
                Editor.Register(delegate { Editor.StartSequence(new WhenTrigger("")); })));

            list.Entries.Add(new ChoiceEntry("Every so often", false,
                Editor.Register(delegate { Editor.StartSequence(new EveryTrigger(Cadences.Default)); })));

            return list;
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

            if (_list != null)
                SequenceSummary.Fill(_list.Entries, _sequences, Editor.World, Editor.Runner, Editor.CurrentTick);
        }

        void Collect()
        {
            _sequences.Clear();
            var sequences = Editor.Program.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _sequences.Add(sequences[i]);
        }
    }
}
