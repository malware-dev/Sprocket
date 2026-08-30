using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;

namespace Reactor.Screens.Sequences
{
    /// <summary>One sentence: a lead-in, then the slots that complete it, wrapping when it has to.</summary>
    public sealed class SentenceView
    {
        readonly Wrap _wrap = new Wrap();
        readonly Label _lead;
        readonly List<Element> _slots = new List<Element>();

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

        public void Update(string lead, List<SentenceSlot> slots)
        {
            _lead.Text = lead;

            for (int i = 0; i < _slots.Count && i < slots.Count; i++)
                Apply(_slots[i], slots[i]);
        }

        public void Collect(List<Hit> hits, List<SentenceSlot> slots)
        {
            for (int i = 0; i < _slots.Count && i < slots.Count; i++)
                hits.Add(Hit.On(_slots[i], slots[i].Command));
        }

        /// <summary>
        /// Puts a slot's words on it, and says whether it is still waiting to be filled.
        /// </summary>
        /// <remarks>
        /// Colour alone marks an unfilled slot, because the game ships one weight and there is no italic
        /// to lean it into. That is why the empty ones take the warning colour rather than a paler ink.
        /// </remarks>
        public static void Apply(Element element, SentenceSlot model)
        {
            var slot = element as Slot;
            if (slot != null)
            {
                slot.Text = model.Text;
                slot.Color = model.IsEmpty ? Palette.Notice : Palette.Ink;
                slot.Underline = model.IsEmpty ? Palette.Notice : Palette.SlotEdge;
                return;
            }

            var word = element as Label;
            if (word != null)
                word.Text = model.Text;
        }
    }
}
