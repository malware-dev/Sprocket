using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The values what is chosen takes, as a column after the stages, with the way to finish the pick.</summary>
    /// <remarks>
    /// Done stands beside the heading where there are no values, and under the last of them where there are, so it is
    /// always the last thing reading down. Why it cannot be finished yet wraps downwards beside it, so Done stays where it is
    /// whatever the reason says.
    /// </remarks>
    public class ValuesColumn : VStack
    {
        readonly HStack _head;
        readonly HStack _foot;
        readonly Label _why;
        readonly Button _done;
        readonly FieldSet _form;

        float _width;

        /// <summary>An empty column <paramref name="width" /> across, headed <paramref name="title" />, finished by <paramref name="done" />.</summary>
        public ValuesColumn(float width, string title, string done)
        {
            Spacing = 8f;
            Style = Styles.Column;
            Width = width;

            // Headed as a stage is, with the line a stage keeps under its heading, so the first value lines up with the
            // boxes beside it.
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new VStack
                    {
                        Spacing = 8f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new Label { Style = Styles.ColumnTitle, Text = title.ToUpperInvariant() },
                            new Label { Style = Styles.ColumnNote }
                        }
                    }
                }
            }.As(out _head));
            Children.Add(new FieldSet().As(out _form));
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children = { new Label { Style = Styles.Refusal, Wraps = true, Placement = Flex.Grow }.As(out _why) }
            }.As(out _foot));

            _done = new Button { Style = Styles.Primary, Text = done };
            _foot.Children.Add(_done);

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

        /// <summary>Where the values are given.</summary>
        public FieldSet Form => _form;

        /// <summary>The way to finish the pick.</summary>
        public Button Done => _done;

        /// <summary>Why the pick cannot be finished as it stands, or empty.</summary>
        public string Why
        {
            get { return _why.Text; }
            set { _why.Text = value ?? ""; }
        }

        /// <summary>Puts Done under the values where there are any, and beside the heading where there are none.</summary>
        public void Place()
        {
            var under = _form.Count > 0;
            if (under && _done.Parent != _foot)
                _foot.Children.Add(_done);
            else if (!under && _done.Parent != _head)
                _head.Children.Add(_done);
        }
    }
}
