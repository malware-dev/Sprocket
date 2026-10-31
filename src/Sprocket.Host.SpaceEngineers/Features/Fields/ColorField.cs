using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A color on one line, shown as the color itself, the picker hanging open under it when pressed.</summary>
    /// <remarks>
    /// As wide whatever the color, so the line never changes width. The picker gives the field its color when Done is pressed
    /// in it, or Enter; put away any other way, it gives nothing, and the field goes on showing the color it held.
    /// </remarks>
    public class ColorField : HStack, IField
    {
        /// <summary>How wide the color is shown.</summary>
        const float SwatchWide = 96f;

        readonly ColorPicker _picker;
        readonly Swatch _swatch;
        readonly Popup _popup;
        string _text = "";

        /// <summary>A field holding no color, its picker offering what <paramref name="offer" /> holds, its notes in <paramref name="texts" />.</summary>
        public ColorField(ColorOffer offer, Texts texts)
        {
            Base.Spacing = Gaps.Parts;
            Style = Styles.ListLine;
            Focusable = true;

            var done = new RelayCommand(Done);
            _picker = new ColorPicker(offer, done, texts) { Placement = FixedSize.Width(ColorPicker.Wide) };

            Children.Add(new Swatch { Style = Styles.ColorSample, Placement = FixedSize.Size(SwatchWide, 20f) }.As(out _swatch));
            Children.Add(ListField.Fold());

            _popup = new Popup
            {
                Style = Styles.Popup,
                Anchor = this,
                Base = { Gap = 4f },
                // The box of its own keeps the popup as wide as the picker.
                Content = new VStack
                {
                    Base = { Spacing = Gaps.Parts },
                    Children =
                    {
                        new HStack { Children = { _picker } },
                        new Button { Style = Styles.Primary, Text = texts.ChromeDone, Align = Align.Right, Command = done }
                    }
                }
            };
            Command = new RelayCommand(() =>
            {
                if (!_popup.IsOpen)
                {
                    _picker.Text = _text;
                    _picker.Recall();
                }

                _popup.Toggle();
            });
            Shown();
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                Shown();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set
            {
                Set(LookStates.Wrong, value);
                _picker.IsWrong = value;
            }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>The picker hung open under it, made with it.</summary>
        public ColorPicker Picker => _picker;

        /// <summary>Whether the picker is hanging open.</summary>
        public bool IsOpen => _popup.IsOpen;

        /// <summary>Takes the color the picker holds as the field's, puts it first among the colors chosen last where it is new, and puts the picker away.</summary>
        void Done()
        {
            var given = _picker.Text;
            _popup.Close();
            if (given == _text)
                return;

            _picker.Remember();
            _text = given;
            Shown();

            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>Shows the color it holds, its swatch filled and its code under the pointer, or the swatch's edge alone where none is.</summary>
        void Shown()
        {
            Tip = CodexTip.For(Tip, _text.Length > 0 ? _text : null);

            Color color;
            byte alpha;
            _swatch.Color = ColorChannels.Colour(_text, out color, out alpha) ? color : Palette.None;
        }
    }
}
