using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A screen's heading: one nameplate per part, with something drawn between them.
    /// </summary>
    /// <remarks>
    /// Core hands over a list rather than a sentence, and this is why: each part answers for itself
    /// whether it can be renamed, so each one gets a control of its own and wears its own pencil. A
    /// heading drawn as one string would have to pick one answer for the whole line, which is exactly
    /// what a sequence cannot do - what starts it is not somebody's to name, and what it then does is.
    /// <para>
    /// What goes between the parts is this target's word and not core's. An arrow, because the two halves
    /// of a sequence's heading are a cause and its consequence - which is what the same arrow already
    /// means everywhere else on these screens.
    /// </para>
    /// </remarks>
    public sealed class HeadingRow
    {
        readonly Stack _row = Widgets.RowOf(Space.Row);
        readonly List<Nameplate> _plates = new List<Nameplate>();
        readonly List<Glyph> _joins = new List<Glyph>();
        readonly List<Label> _leads = new List<Label>();

        /// <summary>
        /// How many parts are on show, which is not how many plates exist.
        /// </summary>
        /// <remarks>
        /// The plates are a pool and it only ever grows, so a screen with one part after a screen with two
        /// leaves a spare behind. Walking the pool instead of the parts is what that spare is for, and it
        /// read one part past the end of the list the first time somebody backed out of a sequence.
        /// </remarks>
        int _shown;

        /// <summary>What the row was last built for, so it is only rebuilt when it would differ.</summary>
        int _shape = -1;

        public Element Root
        {
            get { return _row; }
        }

        public int Count
        {
            get { return _shown; }
        }

        public Nameplate this[int index]
        {
            get { return _plates[index]; }
        }

        /// <summary>
        /// Brings the plates into line with the parts, making or dropping them if the count changed.
        /// </summary>
        /// <remarks>
        /// The part being renamed is left alone. Whatever is in that box is the truth for as long as
        /// somebody is typing into it, and writing core's answer back over it every tick would fight the
        /// caret for it - the same rule the search boxes follow.
        /// </remarks>
        public void Update(List<HeadingPart> parts, int renaming)
        {
            Build(parts);

            for (int i = 0; i < _shown; i++)
            {
                HeadingPart part = parts[i];
                Nameplate plate = _plates[i];

                if (i != renaming)
                    plate.Text = part.Text;

                // Lit only where there is something to press. A pencil on a part nobody can rename would
                // name an act and then refuse it, and the trigger half of a sequence is exactly that part.
                plate.PenColor = part.CanRename ? Palette.Faint : Palette.None;
                plate.Highlight = part.CanRename && i != renaming ? Palette.Panel : Palette.None;

                _leads[i].Text = part.Lead;
            }
        }

        /// <summary>
        /// A number that changes whenever the row has to be built differently.
        /// </summary>
        /// <remarks>
        /// The count alone is not enough: two parts joined by an arrow and two parts where the second
        /// brings its own words are the same count and a different row. Rebuilding is cheap and rare, so
        /// the cheapest honest key is the shape rather than a comparison of everything in it.
        /// </remarks>
        static int Shape(List<HeadingPart> parts)
        {
            int shape = parts.Count;
            for (int i = 0; i < parts.Count; i++)
                shape = shape * 2 + (string.IsNullOrEmpty(parts[i].Lead) ? 0 : 1);

            return shape;
        }

        void Build(List<HeadingPart> parts)
        {
            int shape = Shape(parts);
            if (_shape == shape)
                return;

            _shape = shape;
            _shown = parts.Count;
            _row.Children.Clear();

            while (_plates.Count < parts.Count)
            {
                _plates.Add(Widgets.Nameplate(Lettering.Title));
                _joins.Add(Widgets.Symbol(Symbols.Arrow, Palette.Faint));

                // Quieter than the name it leads into, and the same size. It is the part of the heading
                // nobody chose - saying it as loudly as the name would make the boilerplate the subject.
                _leads.Add(Widgets.Label("", Palette.Muted, Lettering.Title));
            }

            for (int i = 0; i < parts.Count; i++)
            {
                bool lead = !string.IsNullOrEmpty(parts[i].Lead);

                // One connective, never two. A part that brings its own words does not also get an arrow.
                if (i > 0 && !lead)
                    _row.Children.Add(_joins[i]);

                if (lead)
                {
                    // One line, like everything else on this row. It does not have to ask for that any
                    // more - a label is a line tall whatever is written in it, which is what lets a lead
                    // and the name beside it share a baseline without either knowing about the other.
                    _leads[i].Wraps = false;
                    _row.Children.Add(_leads[i]);
                }

                _row.Children.Add(_plates[i]);
            }
        }
    }
}
