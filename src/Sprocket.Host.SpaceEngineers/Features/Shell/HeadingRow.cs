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
    /// <summary>A screen's heading: the names of its parts, with an arrow between them.</summary>
    public class HeadingRow : HStack
    {
        readonly Action<int> _renames;
        readonly List<Part> _parts = new List<Part>();
        readonly List<HeadingPart> _laid = new List<HeadingPart>();

        int _shown;

        int _shape = -1;

        /// <summary>A heading whose parts <paramref name="renames" /> renames, given the index of the part pressed.</summary>
        public HeadingRow(Action<int> renames)
        {
            _renames = renames;

            Spacing = Gaps.Within;
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

                shown.Plate.Text = part.Text;
                shown.Plate.Emphasized = part.Emphasized;
                shown.Plate.CanRename = part.CanRename;
                shown.Renames.CanExecute = part.CanRename && i != renaming;
                shown.Plate.Command = part.CanRename || part.Opens == null ? shown.Renames : part.Opens;

                shown.Plate.About = part.CanRename || part.Opens != null ? part.About : null;

                shown.Mark.Tip = Tip.For(shown.Mark.Tip, part.MarkAbout);
                shown.Lead.Text = part.Lead;
                shown.After.Text = part.After;
            }
        }

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>The name and pencil of the part at <paramref name="index" />, which a press on renames it.</summary>
        public NamePlate Plate(int index)
        {
            return _parts[index].Plate;
        }

        /// <summary>Which part of those shown <paramref name="plate" /> belongs to, or -1 where it is none of them.</summary>
        public int IndexOf(NamePlate plate)
        {
            for (var i = 0; i < _shown; i++)
            {
                if (ReferenceEquals(_parts[i].Plate, plate))
                    return i;
            }

            return -1;
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

        /// <summary>Lays the row out again where the parts, or which of them have words before or after, have changed.</summary>
        /// <remarks>A page makes new parts each time it lays its heading out, so parts other than those laid out are another heading.</remarks>
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
            public readonly NamePlate Plate;
            public readonly Label After;
            public readonly Relay Renames;

            public Part(HeadingRow row, int index)
            {
                Renames = new Relay(() => row.Rename(index));

                Join = new Glyph { Symbol = Symbols.Arrow, Style = Styles.Join, Placement = FixedSize.Size(32f, 32f) };
                Mark = new Glyph { Base = { Padding = EditorWorkspace.LevelWithCapitals }, Style = Styles.Join, Placement = FixedSize.Size(32f, 32f) };
                Lead = new Label { Style = Styles.TitleLead };
                After = new Label { Style = Styles.TitleLead };

                Plate = new NamePlate(true) { Command = Renames };
            }
        }
    }
}
