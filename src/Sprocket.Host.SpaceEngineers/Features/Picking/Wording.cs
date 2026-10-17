using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The title of a pick, said as the sentence its answer makes, worked out again only when the answer changes.</summary>
    public sealed class Wording
    {
        readonly SprocketEditor _editor;
        readonly Offers _offers;
        readonly string _title;
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly List<long> _saidBlocks = new List<long>();
        string _said;
        string _saidChosen = "";
        ToolbarSlot _saidSlot;
        int _saidOffered;
        Quantifier _saidQuantifier;
        bool _retitled;

        /// <summary>
        /// The title of a pick in <paramref name="editor" /> choosing among <paramref name="offers" />, called
        /// <paramref name="title" /> while nothing is picked: of what starts something where <paramref name="forEvents" />,
        /// and of what an if checks where <paramref name="forTests" />.
        /// </summary>
        public Wording(SprocketEditor editor, Offers offers, string title, bool forEvents, bool forTests)
        {
            _editor = editor;
            _offers = offers;
            _title = title;
            _forEvents = forEvents;
            _forTests = forTests;
        }

        /// <summary>The title as the answer stands, or null before it is first worked out.</summary>
        public string Said => _said;

        /// <summary>Whether the title has changed since this was last asked.</summary>
        public bool Take()
        {
            var retitled = _retitled;
            _retitled = false;
            return retitled;
        }

        /// <summary>Works the title out again for the answer: <paramref name="id" /> reached as <paramref name="form" />, through <paramref name="slot" />, of <paramref name="selection" /> read by <paramref name="quantifier" />, a sum or an average where <paramref name="totals" />; only where it has changed since it was last worked out.</summary>
        /// <remarks>Asked after every command and every tick, so it compares before it builds: a tick where nothing was picked makes no string.</remarks>
        public void Reword(string id, Nature form, ToolbarSlot slot, Quantifier quantifier, bool totals, List<long> selection)
        {
            if (_said != null && _saidChosen == id && ToolbarSlot.Same(_saidSlot, slot) && _saidOffered == _offers.Ids.Count
                && _saidQuantifier == quantifier && Same(_saidBlocks, selection))
            {
                return;
            }

            _saidChosen = id;
            _saidSlot = slot;
            _saidOffered = _offers.Ids.Count;
            _saidQuantifier = quantifier;
            _saidBlocks.Clear();
            _saidBlocks.AddRange(selection);

            var at = id.Length == 0 ? -1 : _offers.At(id, form);
            var pressed = slot == null ? null : Occupancy.Pressing(_editor.World, selection, id);
            var chosen = at < 0 ? "" : pressed != null ? pressed.At(slot).Text : _offers.Names[at];

            // A step acts on every block it names, so its blocks are always joined with "and". Only what starts something can
            // be any one of them, and then only where it was asked.
            var blocks = SentenceText.Entities(_editor.World, selection, !_forEvents && !_forTests || quantifier != Quantifier.Any);

            _said = selection.Count == 0 && chosen.Length == 0
                ? _title
                : _forTests
                    ? SentenceText.Testing(blocks, chosen, IsState(id), selection.Count > 1 ? SentenceText.Quantified(quantifier) : "")
                    : _forEvents && (form == Nature.Value || totals)
                        ? SentenceText.Testing(blocks, chosen, false, selection.Count > 1 ? SentenceText.Quantified(quantifier) : "", "When")
                        : SentenceText.Picking(_forEvents, blocks, chosen, at >= 0 && _offers.Sets[at]);

            _retitled = true;
        }

        static bool Same(List<long> one, List<long> other)
        {
            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (one[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>Whether <paramref name="id" /> checks the blocks' status for one of its states, which reads as the blocks themselves.</summary>
        static bool IsState(string id)
        {
            string value, state;
            return Comparing.Split(id, out value, out state);
        }
    }
}
