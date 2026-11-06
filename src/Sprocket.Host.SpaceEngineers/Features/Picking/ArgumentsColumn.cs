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
        readonly VStack _head;
        readonly Label _title;
        readonly Label _why;
        readonly RichText _about;
        readonly FieldSet _form;
        readonly VStack _rest;

        float _width;
        bool _inset;

        /// <summary>An empty column <paramref name="width" /> across, headed <paramref name="title" />.</summary>
        public ArgumentsColumn(float width, string title)
        {
            Base.Spacing = Gaps.Parts;
            Style = Styles.Column;
            Width = width;

            // Headed as a stage is, so the first argument lines up with the boxes beside it.
            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new HStack
                    {
                        Placement = FixedSize.Height(StageColumn.HeadHigh),
                        Children = { new Label { Style = Styles.H1, Text = title.ToUpperInvariant() }.As(out _title) }
                    },
                    new Label { Style = Styles.ColumnNote }
                }
            }.As(out _head));
            Children.Add(new RichText { Style = Styles.MemberAbout, IsVisible = false }.As(out _about));
            Children.Add(new FieldSet().As(out _form));
            Children.Add(new Label { Style = Styles.Refusal, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _why));

            // The rest of the strip's height, so the column stands as tall as the stages beside it.
            Children.Add(new VStack { Placement = Flex.Grow }.As(out _rest));
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
                if (!_inset)
                    Placement = FixedSize.Width(value);
            }
        }

        /// <summary>
        /// Whether it stands in a stage, over that stage's rows and as part of that column, rather than as a column of its own:
        /// unheaded, since what it asks for is the row chosen under it, and as wide as the stage.
        /// </summary>
        public bool Nested
        {
            get { return _inset; }
            set
            {
                if (_inset == value)
                    return;

                _inset = value;
                Style = value ? null : Styles.Column;
                Placement = value ? null : FixedSize.Width(_width);
                _head.IsVisible = !value;
                _rest.IsVisible = !value;
            }
        }

        /// <summary>What the column is headed by: the name of what was chosen, whose options it asks for.</summary>
        public string Title
        {
            get { return _title.Text; }
            set { _title.Text = (value ?? "").ToUpperInvariant(); }
        }

        /// <summary>What says what the chosen thing is.</summary>
        public RichText About => _about;

        /// <summary>Says what the chosen thing is in <paramref name="text" />, or nothing where that is empty.</summary>
        public void Describe(string text)
        {
            _about.Markdown = text ?? "";
            _about.IsVisible = _about.Markdown.Length != 0;
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
