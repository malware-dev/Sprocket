using System;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A value typed into a box as wide as the form, for words; a number has a <see cref="NumberField" />.</summary>
    public class TypedField : HStack, IField
    {
        readonly TextBox _box;

        /// <summary>An empty box whose Enter carries out <paramref name="submit" />.</summary>
        public TypedField(ICommand submit)
        {
            Children.Add(new TextBox
            {
                Style = Styles.Value,
                Focusable = true,
                Submit = submit,
                Placement = Flex.Grow
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
