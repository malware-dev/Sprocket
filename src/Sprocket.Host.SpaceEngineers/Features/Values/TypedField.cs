using System;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A value typed into a box: as wide as the form where it may be anything, and as wide as a number where it is bounded.</summary>
    /// <remarks>
    /// A box as wide as the form invites a sentence, where a value bounded at thirty holds two digits. Every bounded box is
    /// one width, so the boxes of a form start and end together.
    /// </remarks>
    public class TypedField : HStack, IField
    {
        const float Numbers = 176f;

        readonly TextBox _box;

        /// <summary>An empty box, bounded where <paramref name="bounded" /> is set, whose Enter carries out <paramref name="submit" />.</summary>
        public TypedField(bool bounded, ICommand submit)
        {
            Children.Add(new TextBox
            {
                Style = Styles.Value,
                Focusable = true,
                Submit = submit,
                Placement = bounded ? (IPlacement)Fixed.Wide(Numbers) : Flex.Grow
            }.As(out _box));

            _box.Edited += delegate
            {
                var edited = Edited;
                if (edited != null)
                    edited();
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _box.Text; }
            set { _box.Text = value ?? ""; }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _box.Style = value ? Styles.ValueWrong : Styles.Value; }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;
    }
}
