using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
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
        const float Indent = 32f;
        const float SpineIn = 6f;

        // Half the room the list leaves between its rows, so the spines of two rows meet in the middle of it.
        const float Reach = 4f;

        // How far left of the line the spine joining lit lines runs, in the room the number leaves, and so how far it turns in.
        const float JoinOut = 10f;

        readonly List<Label> _joins = new List<Label>();
        readonly List<SlotChip> _chips = new List<SlotChip>();
        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();
        readonly List<Mention> _pieces = new List<Mention>();
        readonly List<PlusMenu> _adders = new List<PlusMenu>();

        int _numbered;
        StepKind _led = (StepKind)(-1);

        /// <summary>An empty row, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public StepRow()
        {
            Slots = new List<SentenceSlot>();
            Adds = new List<AddOption>();
            CreateView();
        }

        /// <summary>Where the step stands in the sequence, counted from one; nothing is shown for none.</summary>
        public int Number { get; set; }

        /// <summary>How many ifs it stands inside, which is how far its sentence is indented.</summary>
        public int Depth { get; set; }

        /// <summary>How many rows after it are carried with it: the rest of the if, for the row that opens one.</summary>
        public int Carries { get; set; }

        /// <summary>The pluses it offers to add to the sequence with, after its sentence, each holding every choice it has: the end of a branch, the row closing an if, an otherwise.</summary>
        public List<AddOption> Adds { get; }

        /// <summary>What sort of step it is, which is what its line opens with.</summary>
        public StepKind Kind { get; set; }

        /// <summary>The parts that complete its sentence.</summary>
        public List<SentenceSlot> Slots { get; }

        /// <summary>What deleting it does, or null while it cannot be deleted.</summary>
        public ICommand Deletes { get; set; }

        /// <summary>What copying it does, or null while it cannot be copied.</summary>
        public ICommand Copies { get; set; }

        /// <summary>Why nothing on it can be changed, said over everything that would change it; empty while it can.</summary>
        public string Locked { get; set; } = "";


        /// <summary>Whether the clock of its wait is lit, tied to the step under the pointer: its underhang's, or its own where it is a wait row.</summary>
        public bool ClockLit { get; set; }

        /// <summary>Whether the clock marks of its sentence are lit, tied to the wait under the pointer.</summary>
        public bool MarksLit { get; set; }

        /// <summary>The color of the ring pulsing around what is lit on it, this tick.</summary>
        public Color Glow { get; set; }

        /// <summary>Whether the spine joining lit lines runs beside the row.</summary>
        public bool Joins { get; set; }

        /// <summary>Whether the spine joining lit lines starts at this row, its first lit line.</summary>
        public bool JoinFirst { get; set; }

        /// <summary>Whether the spine joining lit lines ends at this row, its last lit line.</summary>
        public bool JoinLast { get; set; }

        /// <summary>Whether the pointer is on the step: its grip or any part of its sentence.</summary>
        /// <remarks>Asked every tick, as the pointer gives no word when it moves; a handful of flags on the row, nothing measured.</remarks>
        public bool Hot
        {
            get
            {
                if (_grip.IsHot)
                    return true;

                foreach (var chip in _chips)
                {
                    if (chip.IsHot)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether the step is paused: switched off, kept as it is and passed over.</summary>
        public bool IsOff { get; set; }

        /// <summary>What pausing it, or letting it go again, does, or null while it cannot be paused.</summary>
        public ICommand Pauses { get; set; }

        /// <summary>Whether the sequence is inside this step right now.</summary>
        public bool IsCurrent { get; set; }

        /// <summary>Whether what is wrong with the sequence is on this step.</summary>
        public bool HasProblem { get; set; }

        /// <summary>What is wrong with the step, said under it where <see cref="HasProblem" />.</summary>
        public string Problem { get; set; } = "";

        /// <summary>How much what is on this step matters: a problem washes it red, a warning amber, a note not at all.</summary>
        public NoticeLevel Level { get; set; } = NoticeLevel.Problem;

        /// <summary>Whether the row is being carried to another place.</summary>
        public bool IsCarried { get; set; }

        /// <summary>The handle the row is dragged by.</summary>
        public Grip Grip => _grip;

        /// <summary>Where a spine of the ifs it stands in that ends at it turns in: level with its sentence.</summary>
        public float SpineAt => Bounds.Y + Bounds.Height * 0.5f;

        /// <summary>Shows what it has been told, naming blocks as <paramref name="world" /> calls them and saying its fixed words in <paramref name="texts" />.</summary>
        public void Show(GridWorld world, Texts texts)
        {
            if (_numbered != Number)
            {
                _numbered = Number;
                _number.Text = Number > 0 ? Number.ToString(CultureInfo.InvariantCulture) : "";
            }

            _sentence.Padding = new Thickness(Depth * Indent, 0f, 0f, 0f);
            _problem.Padding = _sentence.Padding;
            _problem.Text = HasProblem ? Problem : "";
            _problem.IsVisible = HasProblem && Problem.Length != 0;
            _problem.Color = RemarkColors.Of(Level, Palette.Faint);

            if (_led != Kind)
            {
                _led = Kind;
                var lead = SentenceText.Lead(Kind);
                _lead.Text = lead;
                _lead.IsVisible = lead.Length != 0;
                _leadWaits.IsVisible = Kind == StepKind.Run || Kind == StepKind.Await;
            }

            // A paused step waits for nothing, so its mark is quieter and is never lit.
            _leadWaits.Style = IsOff ? Styles.WaitsPaused : Styles.Waits;
            _leadWaits.HaloTint = !IsOff && (MarksLit || Kind == StepKind.Await && ClockLit) ? Glow : Palette.None;
            foreach (var chip in _chips)
                chip.Glow = MarksLit && !IsOff ? Glow : Palette.None;

            _sentence.Tip = Tip.For(_sentence.Tip, Kind == StepKind.Run ? texts.MemberWaits : Kind == StepKind.Await ? texts.SequenceAwaitRow : null);

            // The row closing an if and the end of a branch are where things are added, and are nothing that can be moved,
            // paused or deleted; a branch's own line moves, pauses and switches with its if.
            var adds = Kind == StepKind.End || Kind == StepKind.Adds;
            var branch = adds || Kind == StepKind.OtherwiseIf || Kind == StepKind.Otherwise;
            _pause.IsVisible = !branch;
            _delete.IsVisible = !adds;

            // What a drag carries is what a copy holds, so the lines that stay with their if have none.
            _duplicate.IsVisible = !branch || Kind == StepKind.OtherwiseIf;

            if (_chips.Count != Slots.Count || _adders.Count != Adds.Count)
                Reshape();

            for (var i = 0; i < Adds.Count; i++)
                _adders[i].Show(Adds[i]);

            for (var i = 0; i < Slots.Count; i++)
            {
                _chips[i].Texts = texts;
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
                _chips[i].Paused = IsOff;
            }

            // Quiet rather than gone, so a locked row is the same row with nothing on it to press. An otherwise if is carried to
            // another place in its if; the rest of an if's own lines stay with it, so they have no grip to show, and keep the room
            // one takes so their words line up with the rest.
            var fixedInPlace = branch && Kind != StepKind.OtherwiseIf;
            _grip.Color = fixedInPlace ? Palette.None : _grip.CanCarry ? Palette.Faint : Palette.Dim;
            _grip.Tip = Tip.For(_grip.Tip, _grip.CanCarry ? texts.SequenceDragStep : Why());
            _duplicate.Command = Copies;
            _duplicate.IsEnabled = Copies != null;
            _duplicate.Tip = Tip.For(_duplicate.Tip, Copies != null ? texts.SequenceCopyStep : Why());
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
                : HasProblem && Level != NoticeLevel.Info
                    ? Level == NoticeLevel.Warning ? Palette.NoticeWash : Palette.ProblemWash
                    : IsCurrent
                        ? Palette.LiveWash
                        : Palette.None;
        }

        /// <inheritdoc />
        /// <remarks>A wait row has nothing to press, and still lights what it waits for under the pointer.</remarks>
        protected override bool TakesPointer => Kind == StepKind.Await;

        /// <summary>Where the spine of the if <paramref name="level" /> ifs out from the sequence runs down its indent.</summary>
        /// <remarks>The ifs' spines are the list's to draw, since they run past rows that move while one is carried; see <see cref="StepList" />.</remarks>
        public float SpineX(int level)
        {
            return _sentence.Bounds.X + level * Indent + SpineIn;
        }

        /// <summary>Draws the row, then its stretch of the spine joining lit lines where it is joined.</summary>
        protected override void DrawContent(IGraphics g)
        {
            base.DrawContent(g);

            if (Joins)
                Join(g, Bounds.Y - Reach, Bounds.Y + Bounds.Height + Reach);

        }

        /// <summary>
        /// Draws this row's stretch of the spine joining lit lines, turning in at what is lit on it, through the room either side of
        /// the row where it runs on; a row scrolled out of view leaves the spine running off the edge towards it.
        /// </summary>
        void Join(IGraphics g, float top, float bottom)
        {
            var x = _sentence.Bounds.X - JoinOut;
            var line = _sentence.Bounds.Y + _sentence.Bounds.Height * 0.5f;
            var onLine = MarksLit || ClockLit;
            var first = onLine ? line : top;
            var last = onLine ? line : bottom;

            Spine.Down(g, x, JoinFirst ? first : top, JoinLast ? last + Sizes.Spine : bottom, Sizes.Spine, Glow);
            if (onLine)
                g.Fill(new RectangleF(x, line, JoinOut, Sizes.Spine), Glow);
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

            var given = part as ArgumentsSlot;
            if (given != null)
                return given.About(texts);

            if (part is StateSlot)
                return texts.SequenceStateAbout;

            return part is DurationSlot ? texts.SequenceLengthAbout : null;
        }

        /// <summary>The chip <paramref name="part" /> is drawn as, or null where the row does not show it yet.</summary>
        public Element ChipOf(SentenceSlot part)
        {
            var at = Slots.IndexOf(part);
            return at >= 0 && at < _chips.Count ? _chips[at] : null;
        }

        void Part(int at, SentenceSlot part, GridWorld world)
        {
            if (ReferenceEquals(_shown[at], part))
                return;

            _shown[at] = part;

            var join = SentenceText.Joining(Slots, at, Kind == StepKind.If || Kind == StepKind.OtherwiseIf);
            _joins[at].Text = join;
            _joins[at].IsVisible = join.Length != 0;

            _pieces.Clear();
            SentenceText.InStep(world, part, _pieces);
            _chips[at].Show(part, _pieces);
        }

        /// <summary>Lays the sentence out again with a chip for every part it now has and a plus for every adder, keeping the ones it had.</summary>
        void Reshape()
        {
            while (_chips.Count < Slots.Count)
            {
                _joins.Add(new Label { Style = Styles.Phrase, IsVisible = false });
                _chips.Add(new SlotChip());
            }

            while (_adders.Count < Adds.Count)
                _adders.Add(new PlusMenu());

            _adders.RemoveRange(Adds.Count, _adders.Count - Adds.Count);

            var sentence = _sentence.Children;
            sentence.Clear();
            sentence.Add(_leadWaits);
            sentence.Add(_lead);

            for (var i = 0; i < Slots.Count; i++)
            {
                sentence.Add(_joins[i]);
                sentence.Add(_chips[i]);
            }

            foreach (var adder in _adders)
                sentence.Add(adder);

            _joins.RemoveRange(Slots.Count, _joins.Count - Slots.Count);
            _chips.RemoveRange(Slots.Count, _chips.Count - Slots.Count);

            _shown.Clear();
            for (var i = 0; i < Slots.Count; i++)
                _shown.Add(null);
        }
    }
}
