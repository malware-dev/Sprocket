using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>A number between two bounds, set by a slider across the form with its box at the end, the two kept in step.</summary>
    public class SliderField : HStack, IField
    {
        const double Wide = 100;

        const float Box = 96f;

        readonly NumberField _number;
        readonly Slider _slider;
        readonly double _least;
        readonly double _most;
        readonly bool _logarithmic;
        readonly string _format;
        Field _field;

        /// <summary>A slider and box for what <paramref name="field" /> asks for, whose Enter carries out <paramref name="submit" />.</summary>
        public SliderField(Field field, ICommand submit)
        {
            NumberField.Bound(field.Scaled(field.Least), out _least);
            NumberField.Bound(field.Scaled(field.Most), out _most);
            _field = field;
            _logarithmic = _least > 0 && _most / _least >= Wide;
            _format = field.Entry == FieldInput.Whole ? "0" : field.Entry == FieldInput.Decimal ? "0.0#" : "0.##";

            Base.Spacing = Gaps.Parts;
            Children.Add(new Slider { Placement = Flex.Grow }.As(out _slider));
            Children.Add(new NumberField(field, submit, Box).As(out _number));

            _slider.ValueChanged += delegate
            {
                _number.Text = _field.Unscaled(Spelled(Valued(_slider.Value)));
                Raise();
            };

            _number.Edited += delegate
            {
                Place(_field.Scaled(_number.Text));
                Raise();
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _number.Text; }
            set
            {
                _number.Text = value;
                Place(_field.Scaled(value));
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _number.IsWrong = value; }
        }

        /// <inheritdoc />
        public Element Keyboard => _number.Keyboard;

        /// <summary>Whether a field takes a number between two bounds, the bottom below the top.</summary>
        public static bool Takes(Field field)
        {
            double least, most;
            return NumberField.Takes(field) && NumberField.Bound(field.Least, out least) && NumberField.Bound(field.Most, out most)
                   && most > least;
        }

        /// <summary>The value at <paramref name="fraction" /> of the way along the slider, at the nearest step where it moves in steps.</summary>
        double Valued(float fraction)
        {
            var value = _logarithmic ? _least * Math.Pow(_most / _least, fraction) : _least + (_most - _least) * fraction;
            return _field.Step > 0 ? Math.Round(value / _field.Step) * _field.Step : value;
        }

        /// <summary>Puts the grip where <paramref name="said" /> lies between the bounds, and leaves it where it is for anything that is not a number.</summary>
        void Place(string said)
        {
            double value;
            if (string.IsNullOrEmpty(said) || !double.TryParse(said, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return;

            value = Math.Max(_least, Math.Min(_most, value));
            _slider.Value = (float)(_logarithmic
                ? Math.Log(value / _least) / Math.Log(_most / _least)
                : (value - _least) / (_most - _least));
        }

        /// <summary><paramref name="value" /> as the box writes it: whole where the value is whole, else to a few places.</summary>
        string Spelled(double value)
        {
            return value.ToString(_format, CultureInfo.InvariantCulture);
        }

        void Raise()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }
    }
}
