using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Words;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One step as the line it reads as: a lead-in, then the slots that complete it, wrapping when it
    /// has to.
    /// </summary>
    /// <remarks>
    /// <b>The words are worked out only when the sentence changes, never per tick.</b> Turning a slot
    /// into words joins names and builds strings, and this runs sixty times a second - so what was
    /// last put on each element is remembered, and a slot core handed over again unchanged is left
    /// alone. Core rebuilds a sentence by making new slots, so being the same object is the same
    /// question as being unchanged.
    /// <para>
    /// A slot is a chip whether or not it can be pressed, so a sentence stands the same size in the
    /// same place when its sequence locks. One that cannot be pressed loses its bright fill, which is
    /// what marks a control, and its underline with it.
    /// </para>
    /// </remarks>
    public sealed class SentenceView
    {
        readonly Wrap _wrap = new Wrap();
        readonly Label _lead;
        readonly List<Slot> _slots = new List<Slot>();

        /// <summary>The word before each slot, drawn like the lead and hidden where there is none.</summary>
        readonly List<Label> _joins = new List<Label>();

        /// <summary>What each element was last given, so an unchanged one is not worded again.</summary>
        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();

        /// <summary>The step the lead was last worked out from.</summary>
        SentenceStep _shownStep;

        public SentenceView(int slots)
        {
            _wrap.ItemSpacing = Space.Item;
            _wrap.LineSpacing = Space.Row;

            _lead = Widgets.Label("", Palette.Muted, Lettering.Body);
            _lead.Wraps = false;
            _wrap.Children.Add(_lead);

            for (int i = 0; i < slots; i++)
            {
                Label join = Widgets.Label("", Palette.Muted, Lettering.Body);
                join.Wraps = false;
                join.Collapsed = true;
                _joins.Add(join);
                _wrap.Children.Add(join);

                var slot = new Slot();
                _slots.Add(slot);
                _shown.Add(null);
                _wrap.Children.Add(slot);
            }
        }

        public Element Root
        {
            get { return _wrap; }
        }

        public bool Fits(int slots)
        {
            return _slots.Count == slots;
        }

        public void Update(GridWorld world, SentenceStep step)
        {
            if (!ReferenceEquals(_shownStep, step))
            {
                _shownStep = step;
                _lead.Text = SentenceText.Lead(world, step);

                for (int i = 0; i < _joins.Count; i++)
                {
                    _joins[i].Text = SentenceText.Joining(step.Slots, i);
                    _joins[i].Collapsed = _joins[i].Text.Length == 0;
                }
            }

            for (int i = 0; i < _slots.Count && i < step.Slots.Count; i++)
            {
                // Every tick, unlike the words: a slot core handed back unchanged can still have
                // stopped being something to press, which is how a sequence locks itself while it runs.
                SentenceSlot model = step.Slots[i];
                _slots[i].Press = Deed.Sends(model.Command);

                if (!ReferenceEquals(_shown[i], model))
                {
                    _shown[i] = model;
                    Apply(_slots[i], model, SentenceText.InStep(world, model));
                }

                // The bright fill is what says a chip can be pressed. One that cannot keeps its place and
                // its shape, lifted only a notch off the page so the words still read as one part.
                bool pressable = model.Command != 0;
                _slots[i].Background = pressable ? Palette.SlotFill : Palette.Chrome;
                _slots[i].Underline = pressable ? Edge(model) : Palette.None;
            }
        }

        static Color Edge(SentenceSlot model)
        {
            return model.IsEmpty ? Palette.Notice : Palette.SlotEdge;
        }

        /// <summary>
        /// Puts a slot's words on it, and says whether it is still waiting to be filled.
        /// </summary>
        /// <remarks>
        /// Colour alone marks an unfilled slot, because the game ships one weight and there is no italic
        /// to lean it into. That is why the empty ones take the warning colour rather than a paler ink.
        /// </remarks>
        public static void Apply(Element element, SentenceSlot model, string text)
        {
            var slot = element as Slot;
            if (slot != null)
            {
                slot.Text = text;
                slot.Sample = model.Sample;
                slot.Color = model.IsEmpty ? Palette.Notice : Palette.Ink;
                slot.Underline = Edge(model);
                return;
            }

            var word = element as Label;
            if (word == null)
                return;

            word.Text = text;
            word.Sample = model.Sample;
        }
    }
}
