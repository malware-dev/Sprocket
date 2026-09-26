using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Images;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>A screen's heading: each part's name, renamed in place where it can be, with an arrow between parts.</summary>
    public class HeadingRow : HStack
    {
        readonly IScreenHost _host;
        readonly ICommand _commit;
        readonly ICommand _cancel;
        readonly Action _edited;
        readonly List<Part> _parts = new List<Part>();

        List<HeadingPart> _heading;

        int _shown;

        int _shape = -1;

        /// <summary>A heading whose rename presses go to <paramref name="host" />, and whose boxes take <paramref name="commit" /> on Enter, <paramref name="cancel" /> on Escape, and say each edit to <paramref name="edited" />.</summary>
        public HeadingRow(IScreenHost host, ICommand commit, ICommand cancel, Action edited)
        {
            _host = host;
            _commit = commit;
            _cancel = cancel;
            _edited = edited;

            Spacing = 4f;
            Placement = Flex.Grow;
        }

        /// <summary>How many parts are on show.</summary>
        public int Count
        {
            get { return _shown; }
        }

        /// <summary>Shows <paramref name="heading" />, leaving the words of the part at <paramref name="renaming" /> to its box.</summary>
        public void Update(List<HeadingPart> heading, int renaming)
        {
            _heading = heading;
            Build(heading);

            for (var i = 0; i < _shown; i++)
            {
                var part = heading[i];
                var shown = _parts[i];

                shown.Name.Text = part.Text;
                shown.Pen.IsVisible = part.CanRename;
                shown.Pen.Tint = i == renaming ? Palette.None : Palette.Faint;
                shown.Renames.CanExecute = part.CanRename && i != renaming;

                // While a part is renamed, its plate says only why a name is refused. See Refuse.
                if (i != renaming)
                    shown.Plate.Tip = Tip.For(shown.Plate.Tip, part.CanRename ? part.About : null);

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

        static int Shape(List<HeadingPart> heading)
        {
            var shape = heading.Count;

            for (var i = 0; i < heading.Count; i++)
            {
                shape = shape * 2 + (string.IsNullOrEmpty(heading[i].Lead) ? 0 : 1);
                shape = shape * 2 + (string.IsNullOrEmpty(heading[i].After) ? 0 : 1);
            }

            return shape;
        }

        /// <summary>Lays the row out again where the parts, or which of them have words before or after, have changed.</summary>
        /// <remarks>An arrow joins two parts only where the second has no words of its own to lead with.</remarks>
        void Build(List<HeadingPart> heading)
        {
            var shape = Shape(heading);
            if (_shape == shape)
                return;

            _shape = shape;
            _shown = heading.Count;
            Children.Clear();

            while (_parts.Count < heading.Count)
                _parts.Add(new Part(this, _parts.Count));

            for (var i = 0; i < heading.Count; i++)
            {
                var part = _parts[i];
                var lead = !string.IsNullOrEmpty(heading[i].Lead);

                if (i > 0 && !lead)
                    Children.Add(part.Join);

                if (lead)
                    Children.Add(part.Lead);

                Children.Add(part.Plate);

                if (!string.IsNullOrEmpty(heading[i].After))
                    Children.Add(part.After);
            }
        }

        void Rename(int index)
        {
            var part = _heading[index];
            _host.Send(_parts[index].Plate, part.RenameCommand, part.Prompt);
        }

        /// <summary>The elements that show one part of the heading.</summary>
        sealed class Part
        {
            public readonly Glyph Join;
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

                Join = new Glyph { Symbol = Symbols.Arrow, Style = Styles.Join, Placement = Fixed.Size(32f, 32f) };
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
                        new Glyph { Symbol = Symbols.Pencil, Style = Styles.Pen, Placement = Fixed.Size(32f, 32f) }.As(out Pen)
                    }
                };

                Box.Edited += row._edited;

            }
        }
    }
}
