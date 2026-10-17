using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A screen's heading: each part's name, renamed in place where it can be or pressed to open what it names, with an arrow between parts.</summary>
    public class HeadingRow : HStack
    {
        readonly Action<int> _renames;
        readonly ICommand _commit;
        readonly ICommand _cancel;
        readonly Action _edited;
        readonly List<Part> _parts = new List<Part>();
        readonly List<HeadingPart> _laid = new List<HeadingPart>();

        int _shown;

        int _shape = -1;

        /// <summary>A heading whose parts are renamed by <paramref name="renames" />, handed the part's place, and whose boxes take <paramref name="commit" /> on Enter, <paramref name="cancel" /> on Escape, and say each edit to <paramref name="edited" />.</summary>
        public HeadingRow(Action<int> renames, ICommand commit, ICommand cancel, Action edited)
        {
            _renames = renames;
            _commit = commit;
            _cancel = cancel;
            _edited = edited;

            Spacing = 4f;
            Placement = Flex.Grow;
        }

        /// <summary>How many parts are on show.</summary>
        public int Count => _shown;

        /// <summary>Shows <paramref name="heading" />, leaving the words of the part at <paramref name="renaming" /> to its box.</summary>
        public void Update(List<HeadingPart> heading, int renaming)
        {
            Build(heading);

            for (var i = 0; i < _shown; i++)
            {
                var part = heading[i];
                var shown = _parts[i];

                shown.Name.Text = part.Text;
                shown.Pen.IsVisible = part.CanRename;
                shown.Pen.Tint = i == renaming ? Palette.None : Palette.Faint;
                shown.Renames.CanExecute = part.CanRename && i != renaming;
                shown.Plate.Command = part.CanRename || part.Opens == null ? shown.Renames : part.Opens;

                // While a part is renamed, its plate says only why a name is refused. See Refuse.
                if (i != renaming)
                    shown.Plate.Tip = Tip.For(shown.Plate.Tip, part.CanRename || part.Opens != null ? part.About : null);

                shown.Mark.Tip = Tip.For(shown.Mark.Tip, part.MarkAbout);
                shown.Lead.Text = part.Lead;
                shown.After.Text = part.After;
            }
        }

        /// <inheritdoc />
        /// <remarks>Always: a heading longer than the header is cut off rather than drawn over the header's controls.</remarks>
        protected override bool Clips => true;

        /// <summary>The box the part at <paramref name="index" /> is renamed in.</summary>
        public TextBox Box(int index)
        {
            return _parts[index].Box;
        }

        /// <summary>The name and pencil of the part at <paramref name="index" />, which a press on renames it.</summary>
        public Element Plate(int index)
        {
            return _parts[index].Plate;
        }

        /// <summary>Puts <paramref name="name" /> in the part's box, all of it selected, in place of its words and its pencil.</summary>
        public void Begin(int index, string name)
        {
            var part = _parts[index];

            part.Pen.Tint = Palette.None;
            part.Renames.CanExecute = false;
            part.Plate.Tip = null;
            part.Name.IsVisible = false;
            part.Box.IsVisible = true;
            part.Box.Text = name;
            part.Box.SelectAll();
        }

        /// <summary>Puts the part's words back in place of its box.</summary>
        public void End(int index)
        {
            var part = _parts[index];

            part.Name.IsVisible = true;
            part.Box.IsVisible = false;
            Refuse(index, null);
        }

        /// <summary>Marks the part's box as holding a name that cannot be given, saying <paramref name="why" /> on the box and its pencil's place, or clears the mark for null.</summary>
        public void Refuse(int index, string why)
        {
            var part = _parts[index];

            part.Box.Style = why == null ? Styles.TitleBox : Styles.TitleRefused;
            part.Box.Tip = Tip.For(part.Box.Tip, why);
            part.Plate.Tip = Tip.For(part.Plate.Tip, why);
        }

        /// <summary>Whether <paramref name="heading" /> holds the very parts the row was laid out for.</summary>
        bool Laid(List<HeadingPart> heading)
        {
            if (heading.Count != _laid.Count)
                return false;

            for (var i = 0; i < heading.Count; i++)
            {
                if (!ReferenceEquals(heading[i], _laid[i]))
                    return false;
            }

            return true;
        }

        static int Shape(List<HeadingPart> heading)
        {
            var shape = heading.Count;

            foreach (var part in heading)
            {
                shape = shape * 2 + (string.IsNullOrEmpty(part.Lead) ? 0 : 1);
                shape = shape * 2 + (string.IsNullOrEmpty(part.After) ? 0 : 1);
                shape = shape * 2 + (string.IsNullOrEmpty(part.Mark) ? 0 : 1);
            }

            return shape;
        }

        /// <summary>
        /// Lays the row out again where the heading is another screen's, or the parts, or which of them have words before
        /// or after, have changed.
        /// </summary>
        /// <remarks>
        /// A page makes its heading's parts anew each time it lays its heading out, and only changes their words after, so
        /// parts that are not the ones laid out are another heading, whatever its shape. An arrow joins two parts only where
        /// the second has no words of its own to lead with.
        /// </remarks>
        void Build(List<HeadingPart> heading)
        {
            var shape = Shape(heading);
            if (_shape == shape && Laid(heading))
                return;

            _shape = shape;
            _shown = heading.Count;
            _laid.Clear();
            _laid.AddRange(heading);
            Children.Clear();

            while (_parts.Count < heading.Count)
                _parts.Add(new Part(this, _parts.Count));

            for (var i = 0; i < heading.Count; i++)
            {
                var part = _parts[i];
                var lead = !string.IsNullOrEmpty(heading[i].Lead);

                if (i > 0 && !lead)
                    Children.Add(part.Join);

                // The words leading a part come first, and its mark beside its name: "of", then the logic system's mark and name.
                if (lead)
                    Children.Add(part.Lead);

                if (!string.IsNullOrEmpty(heading[i].Mark))
                {
                    part.Mark.Symbol = heading[i].Mark;
                    Children.Add(part.Mark);
                }

                Children.Add(part.Plate);

                if (!string.IsNullOrEmpty(heading[i].After))
                    Children.Add(part.After);
            }
        }

        void Rename(int index)
        {
            _renames(index);
        }

        /// <summary>The elements that show one part of the heading.</summary>
        sealed class Part
        {
            public readonly Glyph Join;
            public readonly Glyph Mark;
            public readonly Label Lead;
            public readonly HStack Plate;
            public readonly Label Name;
            public readonly TextBox Box;
            public readonly Glyph Pen;
            public readonly Label After;
            public readonly Relay Renames;

            public Part(HeadingRow row, int index)
            {
                Renames = new Relay(() => row.Rename(index));

                Join = new Glyph { Symbol = Symbols.Arrow, Style = Styles.Join, Placement = FixedSize.Size(32f, 32f) };
                Mark = new Glyph { Style = Styles.Join, Placement = FixedSize.Size(32f, 32f), Padding = EditorView.LevelWithCapitals };
                Lead = new Label { Style = Styles.TitleLead };
                After = new Label { Style = Styles.TitleLead };

                Plate = new HStack
                {
                    Style = Styles.Plate,
                    Command = Renames,
                    Children =
                    {
                        new Label { Style = Styles.TitleName }.As(out Name),
                        new TextBox
                        {
                            Style = Styles.TitleBox,
                            IsVisible = false,
                            Submit = row._commit,
                            Cancel = row._cancel
                        }.As(out Box),
                        new Glyph { Symbol = Symbols.Pencil, Style = Styles.Pen, Placement = FixedSize.Size(32f, 32f), Padding = EditorView.LevelWithCapitals }.As(out Pen)
                    }
                };

                Box.Edited += row._edited;

            }
        }
    }
}
