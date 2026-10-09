using Sprocket.Host.SpaceEngineers.Features.Fields;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What the chosen thing is given, its options or the new value, as a column after the stages, with why the pick cannot be finished under them.</summary>
    public class ArgumentsColumn : VStack
    {
        readonly Label _title;
        readonly Label _why;
        readonly FieldSet _form;

        float _width;

        /// <summary>An empty column <paramref name="width" /> across, headed <paramref name="title" />.</summary>
        public ArgumentsColumn(float width, string title)
        {
            Spacing = 8f;
            Style = Styles.Column;
            Width = width;

            // Headed as a stage is, with the line a stage keeps under its heading, so the first argument lines up with the
            // boxes beside it.
            Children.Add(new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.ColumnTitle, Text = title.ToUpperInvariant() }.As(out _title),
                    new Label { Style = Styles.ColumnNote }
                }
            });
            Children.Add(new FieldSet().As(out _form));
            Children.Add(new Label { Style = Styles.Refusal, Wraps = true, IsVisible = false }.As(out _why));

            // The rest of the strip's height, so the column stands as tall as the stages beside it.
            Children.Add(new VStack { Placement = Flex.Grow });
        }

        /// <summary>How wide the column is.</summary>
        public float Width
        {
            get { return _width; }
            set
            {
                if (_width == value)
                    return;

                _width = value;
                Placement = Fixed.Wide(value);
            }
        }

        /// <summary>What the column is headed by: the name of what was chosen, whose options it asks for.</summary>
        public string Title
        {
            get { return _title.Text; }
            set { _title.Text = (value ?? "").ToUpperInvariant(); }
        }

        /// <summary>Where the arguments are given.</summary>
        public FieldSet Form => _form;

        /// <summary>Nothing in it is drawn past its edge: a list or a question it opens hangs in a window of its own.</summary>
        protected override bool Clips => true;

        /// <summary>Why the pick cannot be finished as it stands, or empty.</summary>
        public string Why
        {
            get { return _why.Text; }
            set
            {
                _why.Text = value ?? "";
                _why.IsVisible = _why.Text.Length != 0;
            }
        }
    }
}
