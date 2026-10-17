using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>One trigger as the sentence it reads as: when it happens, then what it does, each part pressed to change it.</summary>
    /// <remarks>
    /// The same line a step of a sequence is drawn as, with the same chips, because it is the same act: words with
    /// the parts somebody chose sitting in them, each one opening the question it was answered with. Whoever holds
    /// it sets what it shows and calls <see cref="Show" />. The elements it is drawn with are declared in
    /// <c>TriggerRow.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class TriggerRow : HStack
    {
        readonly List<SlotChip> _starts = new List<SlotChip>();
        readonly List<Label> _joins = new List<Label>();
        readonly List<Label> _startJoins = new List<Label>();
        readonly List<SlotChip> _does = new List<SlotChip>();
        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();
        readonly List<Mention> _pieces = new List<Mention>();

        string _led;
        string _joined;

        /// <summary>An empty row, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public TriggerRow()
        {
            Starts = new List<SentenceSlot>();
            Does = new List<SentenceSlot>();
            DoesLead = "";
            Problem = "";
            CreateView();
        }

        /// <summary>Which sort of moment starts it, which is what its line opens with.</summary>
        public TriggerKind Kind { get; set; }

        /// <summary>What starts it: the blocks and the moment, or how often it comes round.</summary>
        public List<SentenceSlot> Starts { get; }

        /// <summary>The words between the two halves, "then run" or "then", as the program writes them.</summary>
        public string DoesLead { get; set; }

        /// <summary>What it does: the sequence it is about, or the one act it does itself.</summary>
        public List<SentenceSlot> Does { get; }

        /// <summary>Whether what it watches for is true at this instant.</summary>
        /// <remarks>
        /// Idle while the signal reads true is not a contradiction. It is the difference between a moment and a
        /// situation, shown rather than explained.
        /// </remarks>
        public bool IsLiveNow { get; set; }

        /// <summary>What is wrong with it, or worth a word, in words, or empty.</summary>
        public string Problem { get; set; }

        /// <summary>How much <see cref="Problem" /> matters: a problem, a warning or a note.</summary>
        public NoticeLevel Level { get; set; } = NoticeLevel.Problem;

        /// <summary>What deleting it does, or null where it cannot be deleted.</summary>
        public ICommand Deletes { get; set; }

        /// <summary>What copying it does, or null where it cannot be copied.</summary>
        public ICommand Copies { get; set; }

        /// <summary>Why nothing on it can be changed, said over every part that would change it; empty while it can.</summary>
        public string Locked { get; set; } = "";

        /// <summary>Shows what it has been told, naming blocks as <paramref name="world" /> calls them and saying its fixed words in <paramref name="texts" />.</summary>
        /// <remarks>Runs on every refresh of the page, so it words only a part that has changed.</remarks>
        public void Show(GridWorld world, Texts texts)
        {
            if (_starts.Count != Starts.Count || _does.Count != Does.Count)
                Reshape();

            var lead = SentenceText.Lead(Kind, Starts.Count);
            if (!ReferenceEquals(_led, lead))
            {
                _led = lead;
                _lead.Text = lead;
            }

            if (!ReferenceEquals(_joined, DoesLead))
            {
                _joined = DoesLead;
                _between.Text = DoesLead;
                _between.IsVisible = DoesLead.Length != 0;
            }

            for (var i = 0; i < Starts.Count; i++)
            {
                _starts[i].Tip = Tip.For(_starts[i].Tip, Starts[i].Opens == null ? Why() : Starting(texts, Starts[i]));

                // A number watched reads as a check does: a value on some blocks, then how it compares.
                if (!ReferenceEquals(_shown[i], Starts[i]))
                {
                    var join = SentenceText.Joining(Starts, i, true);
                    _startJoins[i].Text = join;
                    _startJoins[i].IsVisible = join.Length != 0;
                }

                Part(_starts[i], i, Starts[i], world);
            }

            for (var i = 0; i < Does.Count; i++)
            {
                _does[i].Texts = texts;
                _does[i].Tip = Tip.For(_does[i].Tip, Does[i].Opens == null ? Why() : Doing(texts, Does[i]));

                // Before the part is shown, which is what records it as shown: a join is worked out only for a
                // part that has changed.
                if (!ReferenceEquals(_shown[Starts.Count + i], Does[i]))
                {
                    var join = SentenceText.Joining(Does, i);
                    _joins[i].Text = join;
                    _joins[i].IsVisible = join.Length != 0;
                }

                Part(_does[i], Starts.Count + i, Does[i], world);
            }

            _live.Text = texts.TriggerLive;
            _live.IsVisible = IsLiveNow;

            _problem.Text = Problem;
            _problem.IsVisible = Problem.Length != 0;
            _problem.Color = RemarkColors.Of(Level, Palette.Faint);

            // The same wash a broken row in a list gets, so arriving here from one arrives at something
            // already recognised.
            Background = Problem.Length != 0 ? RemarkColors.Wash(Level) : Palette.None;

            // Quiet rather than gone while it cannot be copied or deleted, saying why.
            _duplicate.Command = Copies;
            _duplicate.IsVisible = Copies != null || Locked.Length != 0;
            _duplicate.IsEnabled = Copies != null;
            _duplicate.Tip = Tip.For(_duplicate.Tip, Copies != null ? texts.TriggerCopy : Why());

            _delete.Command = Deletes;
            _delete.IsVisible = Deletes != null || Locked.Length != 0;
            _delete.IsEnabled = Deletes != null;
            _delete.Tip = Tip.For(_delete.Tip, Deletes != null ? texts.TriggerDelete : Why());
        }

        /// <summary>What pressing a part of what starts it does, or null where it goes without saying.</summary>
        static string Starting(Texts texts, SentenceSlot part)
        {
            if (part is EntitiesSlot)
                return texts.TriggerBlocksAbout;

            if (part is DurationSlot)
                return texts.TriggerRateAbout;

            return part is EventSlot ? texts.TriggerWhatAbout : null;
        }

        /// <summary>What pressing a part of what it does does, or null where it goes without saying.</summary>
        static string Doing(Texts texts, SentenceSlot part)
        {
            if (part is SequenceSlot || part is ActionSlot)
                return texts.TriggerDoesAbout;

            if (part is EntitiesSlot)
                return texts.TriggerTargetsAbout;

            var given = part as ArgumentsSlot;
            return given?.About(texts);
        }

        /// <summary><see cref="Locked" />, or null where there is nothing to say.</summary>
        string Why()
        {
            return Locked.Length == 0 ? null : Locked;
        }

        /// <summary>The chip <paramref name="part" /> is drawn as, or null where the row does not show it yet.</summary>
        public Element ChipOf(SentenceSlot part)
        {
            var at = Starts.IndexOf(part);
            if (at >= 0)
                return at < _starts.Count ? _starts[at] : null;

            at = Does.IndexOf(part);
            return at >= 0 && at < _does.Count ? _does[at] : null;
        }

        void Part(SlotChip chip, int at, SentenceSlot part, GridWorld world)
        {
            if (ReferenceEquals(_shown[at], part))
                return;

            _shown[at] = part;
            _pieces.Clear();
            SentenceText.InTrigger(world, part, _pieces);
            chip.Show(part, _pieces);
        }

        void Reshape()
        {
            while (_starts.Count < Starts.Count)
            {
                _startJoins.Add(new Label { Style = Styles.Phrase, IsVisible = false });
                _starts.Add(new SlotChip());
            }

            while (_does.Count < Does.Count)
            {
                _joins.Add(new Label { Style = Styles.Phrase, IsVisible = false });
                _does.Add(new SlotChip());
            }

            var sentence = _sentence.Children;
            sentence.Clear();
            sentence.Add(_lead);

            for (var i = 0; i < Starts.Count; i++)
            {
                sentence.Add(_startJoins[i]);
                sentence.Add(_starts[i]);
            }

            sentence.Add(_between);

            for (var i = 0; i < Does.Count; i++)
            {
                sentence.Add(_joins[i]);
                sentence.Add(_does[i]);
            }

            sentence.Add(_live);

            _starts.RemoveRange(Starts.Count, _starts.Count - Starts.Count);
            _startJoins.RemoveRange(Starts.Count, _startJoins.Count - Starts.Count);
            _joins.RemoveRange(Does.Count, _joins.Count - Does.Count);
            _does.RemoveRange(Does.Count, _does.Count - Does.Count);

            _shown.Clear();
            for (var i = 0; i < Starts.Count + Does.Count; i++)
                _shown.Add(null);
        }
    }
}
