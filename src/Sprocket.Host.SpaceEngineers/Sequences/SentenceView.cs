using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Words;

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
    /// </remarks>
    public sealed class SentenceView
    {
        readonly Wrap _wrap = new Wrap();
        readonly Label _lead;
        readonly List<Element> _slots = new List<Element>();

        /// <summary>What each element was last given, so an unchanged one is not worded again.</summary>
        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();

        /// <summary>The step the lead was last worked out from.</summary>
        SentenceStep _shownStep;

        public SentenceView(bool editable, int slots)
        {
            _wrap.ItemSpacing = Space.Item;
            _wrap.LineSpacing = Space.Row;

            _lead = Widgets.Label("", Palette.Muted, Lettering.Body);
            _lead.Wraps = false;
            _wrap.Children.Add(_lead);

            for (int i = 0; i < slots; i++)
            {
                Element slot;

                if (editable)
                {
                    slot = new Slot();
                }
                else
                {
                    Label word = Widgets.Label("", Palette.Ink, Lettering.Body);
                    word.Wraps = false;
                    slot = word;
                }

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
            }

            for (int i = 0; i < _slots.Count && i < step.Slots.Count; i++)
            {
                // Every tick, unlike the words: a slot core handed back unchanged can still have
                // stopped being something to press, which is how a sequence locks itself while it runs.
                _slots[i].Press = step.Slots[i].Asks
                    ? Deed.Values(step.Slots[i].Command, step.Slots[i].Question,
                        step.Slots[i].Fields, step.Slots[i].Filled)
                    : Deed.Sends(step.Slots[i].Command);

                if (ReferenceEquals(_shown[i], step.Slots[i]))
                    continue;

                _shown[i] = step.Slots[i];
                Apply(_slots[i], step.Slots[i], SentenceText.InStep(world, step.Slots[i]));
            }
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
                slot.Color = model.IsEmpty ? Palette.Notice : Palette.Ink;
                slot.Underline = model.IsEmpty ? Palette.Notice : Palette.SlotEdge;
                return;
            }

            var word = element as Label;
            if (word != null)
                word.Text = text;
        }
    }
}
