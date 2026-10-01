using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>One step of a sequence as the line it reads as: its number, a lead-in, then the parts that complete it.</summary>
    /// <remarks>
    /// A step and the wait for it to finish are one row, moved and deleted together. Whoever holds it sets what it
    /// shows and calls <see cref="Show" />; the words are worked out only for a part that has changed, because this
    /// runs every tick. The elements it is drawn with are declared in <c>StepRow.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class StepRow : HStack
    {
        readonly List<Label> _joins = new List<Label>();
        readonly List<Slot> _chips = new List<Slot>();
        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();
        readonly List<Mention> _pieces = new List<Mention>();

        int _numbered;
        StepKind _led = (StepKind)(-1);

        /// <summary>An empty row, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public StepRow()
        {
            Slots = new List<SentenceSlot>();
            CreateView();
        }

        /// <summary>Where the step stands in the sequence, counted from one.</summary>
        public int Number { get; set; }

        /// <summary>What sort of step it is, which is what its line opens with.</summary>
        public StepKind Kind { get; set; }

        /// <summary>The parts that complete its sentence.</summary>
        public List<SentenceSlot> Slots { get; }

        /// <summary>What deleting it does, or null while it cannot be deleted.</summary>
        public ICommand Deletes { get; set; }

        /// <summary>Why nothing on it can be changed, said over everything that would change it; empty while it can.</summary>
        public string Locked { get; set; } = "";

        /// <summary>Whether the step is paused: switched off, kept as it is and passed over.</summary>
        public bool IsOff { get; set; }

        /// <summary>What pausing it, or letting it go again, does, or null while it cannot be paused.</summary>
        public ICommand Pauses { get; set; }

        /// <summary>Whether the sequence is inside this step right now.</summary>
        public bool IsCurrent { get; set; }

        /// <summary>Whether what is wrong with the sequence is on this step.</summary>
        public bool HasProblem { get; set; }

        /// <summary>Whether the row is being carried to another place.</summary>
        public bool IsCarried { get; set; }

        /// <summary>The handle the row is dragged by.</summary>
        public Grip Grip => _grip;

        /// <summary>Shows what it has been told, naming blocks as <paramref name="world" /> calls them and saying its fixed words in <paramref name="texts" />.</summary>
        public void Show(GridWorld world, Texts texts)
        {
            if (_numbered != Number)
            {
                _numbered = Number;
                _number.Text = Number.ToString(CultureInfo.InvariantCulture);
            }

            if (_led != Kind)
            {
                _led = Kind;
                var lead = SentenceText.Lead(Kind);
                _lead.Text = lead;
                _lead.IsVisible = lead.Length != 0;
            }

            if (_chips.Count != Slots.Count)
                Reshape();

            for (var i = 0; i < Slots.Count; i++)
            {
                _chips[i].WaitsTip = texts.MemberWaits;
                _chips[i].Tip = Tip.For(_chips[i].Tip, Slots[i].Opens == null ? Why() : About(texts, Slots[i]));
                Part(i, Slots[i], world);
            }

            // A paused step reads as quieter than one that runs, and every part of it can still be pressed and changed.
            var words = IsOff ? Styles.PausedPhrase : Styles.Phrase;
            var parts = IsOff ? Styles.PausedSlot : Styles.Slot;
            _lead.Style = words;
            for (var i = 0; i < _chips.Count; i++)
            {
                _joins[i].Style = words;
                _chips[i].Style = parts;
            }

            // Quiet rather than gone, so a locked row is the same row with nothing on it to press.
            _grip.Color = _grip.CanCarry ? Palette.Faint : Palette.Dim;
            _grip.Tip = Tip.For(_grip.Tip, _grip.CanCarry ? texts.SequenceDragStep : Why());
            _delete.Command = Deletes;
            _delete.IsEnabled = Deletes != null;
            _delete.Tip = Tip.For(_delete.Tip, Deletes != null ? texts.SequenceDeleteStep : Why());

            // As quiet as the bin while the step runs, and lit while it is paused, which is the one time it has anything to say.
            _pause.Style = IsOff ? Styles.Held : Styles.Holds;
            _pause.Command = Pauses;
            _pause.IsEnabled = Pauses != null;
            _pause.Tip = Tip.For(_pause.Tip, Pauses == null ? Why() : IsOff ? texts.SequenceUnpauseStep : texts.SequencePauseStep);

            // Broken outranks being the current step, which the two can only be at once for the instant between a
            // block going away and the run abandoning the step that named it.
            Background = IsCarried
                ? Palette.SlotFill
                : HasProblem
                    ? Palette.ProblemWash
                    : IsCurrent
                        ? Palette.LiveWash
                        : Palette.None;
        }

        /// <summary><see cref="Locked" />, or null where there is nothing to say.</summary>
        string Why()
        {
            return Locked.Length == 0 ? null : Locked;
        }

        /// <summary>What pressing <paramref name="part" /> does, or null where it goes without saying.</summary>
        static string About(Texts texts, SentenceSlot part)
        {
            if (part is ActionSlot)
                return texts.SequenceActionAbout;

            if (part is EntitiesSlot)
                return texts.SequenceBlocksAbout;

            if (part is ValuesSlot)
                return texts.SequenceValuesAbout;

            return part is DurationSlot ? texts.SequenceLengthAbout : null;
        }

        void Part(int at, SentenceSlot part, GridWorld world)
        {
            if (ReferenceEquals(_shown[at], part))
                return;

            _shown[at] = part;

            var join = SentenceText.Joining(Slots, at);
            _joins[at].Text = join;
            _joins[at].IsVisible = join.Length != 0;

            _pieces.Clear();
            SentenceText.InStep(world, part, _pieces);
            _chips[at].Show(part, _pieces);
        }

        /// <summary>Lays the sentence out again with a chip for every part it now has, keeping the chips it had.</summary>
        void Reshape()
        {
            while (_chips.Count < Slots.Count)
            {
                _joins.Add(new Label { Style = Styles.Phrase, IsVisible = false });
                _chips.Add(new Slot());
            }

            var sentence = _sentence.Children;
            sentence.Clear();
            sentence.Add(_lead);

            for (var i = 0; i < Slots.Count; i++)
            {
                sentence.Add(_joins[i]);
                sentence.Add(_chips[i]);
            }

            _joins.RemoveRange(Slots.Count, _joins.Count - Slots.Count);
            _chips.RemoveRange(Slots.Count, _chips.Count - Slots.Count);

            _shown.Clear();
            for (var i = 0; i < Slots.Count; i++)
                _shown.Add(null);
        }
    }
}
