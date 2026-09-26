using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

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

        readonly List<Label> _joins = new List<Label>();

        readonly List<SentenceSlot> _shown = new List<SentenceSlot>();
        readonly List<Mention> _pieces = new List<Mention>();

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
                join.IsVisible = false;
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
                    _joins[i].IsVisible = _joins[i].Text.Length != 0;
                }
            }

            for (int i = 0; i < _slots.Count && i < step.Slots.Count; i++)
            {
                // A part that stops being something to press, as every part does when a sequence locks
                // itself to run, is handed over as a new part, so an unchanged one needs nothing.
                SentenceSlot model = step.Slots[i];
                if (ReferenceEquals(_shown[i], model))
                    continue;

                _shown[i] = model;
                _pieces.Clear();
                SentenceText.InStep(world, model, _pieces);
                _slots[i].Show(model, _pieces);
            }
        }
    }
}
