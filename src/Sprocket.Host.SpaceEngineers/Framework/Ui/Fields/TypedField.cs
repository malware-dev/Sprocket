using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>A value in words, typed into a box as wide as the form.</summary>
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
            set { _box.Set(LookStates.Wrong, value); }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;

        /// <summary>What the empty box shows.</summary>
        public string Hint
        {
            get { return _box.Hint; }
            set { _box.Hint = value; }
        }

        /// <summary>How many characters may be typed, or zero for no limit.</summary>
        public int Longest
        {
            get { return _box.MaxLength; }
            set { _box.MaxLength = value; }
        }
    }
}
