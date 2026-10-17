using System;
using System.Globalization;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using VRage.Input;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>A box that holds a number: set against its right-hand end, as wide as the widest number it expects, taking only what can become a number, and stepped by up and down.</summary>
    public class NumberBox : TextBox
    {
        readonly StringBuilder _measuring = new StringBuilder(16);

        string _widest = "00000";
        string _measuredWidest;
        string _measuredFont;
        float _measuredSize;
        float _widestWidth;

        /// <summary>An empty box taking any number of nought or more, five digits wide, stepped by one.</summary>
        public NumberBox()
        {
            Step = 1;
            Format = "0.##########";
        }

        /// <summary>The widest number the box expects to hold, which sets its width.</summary>
        public string Widest
        {
            get { return _widest; }
            set
            {
                value = value ?? "";
                if (_widest == value)
                    return;

                _widest = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether it takes a number below nought.</summary>
        public bool Negative { get; set; }

        /// <summary>Whether it takes only whole numbers.</summary>
        public bool Whole { get; set; }

        /// <summary>How far one press of up or down moves the number.</summary>
        public double Step { get; set; }

        /// <summary>The least up and down move the number to, or null for no floor but nought where <see cref="Negative" /> is unset.</summary>
        public double? Least { get; set; }

        /// <summary>The most up and down move the number to, or null for no ceiling.</summary>
        public double? Most { get; set; }

        /// <summary>How a number moved by up or down is written, as a double is formatted.</summary>
        public string Format { get; set; }

        /// <inheritdoc />
        protected override bool Trails => true;

        /// <inheritdoc />
        /// <remarks>Up and down move the number a step.</remarks>
        public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (!ctrl && (key == MyKeys.Up || key == MyKeys.Down))
            {
                StepBy(key == MyKeys.Up ? 1 : -1);
                return true;
            }

            return base.OnKey(input, key, shift, ctrl);
        }

        /// <summary>Moves the number <paramref name="by" /> steps on from the step nearest what is typed, within its bounds, as an edit.</summary>
        public void StepBy(int by)
        {
            double number;
            if (!double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                number = 0;

            var step = Step > 0 ? Step : 1;
            number = Math.Round(number / step) * step + by * step;

            var least = Least ?? (Negative ? double.MinValue : 0);
            if (number < least)
                number = least;

            if (Most.HasValue && number > Most.Value)
                number = Most.Value;

            SelectAll();
            Insert(number.ToString(Format, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        /// <remarks>Whatever can still become a number: digits, a point unless it is <see cref="Whole" />, and a leading minus where it is <see cref="Negative" />.</remarks>
        protected override bool Takes(string text)
        {
            var point = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '0' && c <= '9')
                    continue;

                if (c == '-' && i == 0 && Negative)
                    continue;

                if (c == '.' && !point && !Whole)
                {
                    point = true;
                    continue;
                }

                return false;
            }

            return base.Takes(text);
        }

        /// <inheritdoc />
        /// <remarks>The widest expected number is measured once each time it, the font or the size changes.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = base.MeasureContent(g, available);

            if (_measuredWidest != _widest || _measuredFont != Font || _measuredSize != FontSize)
            {
                _measuring.Clear();
                _measuring.Append(_widest);
                _widestWidth = g.MeasureText(Font, _measuring, FontSize).X;
                _measuredWidest = _widest;
                _measuredFont = Font;
                _measuredSize = FontSize;
            }

            var width = _widestWidth + CaretWidth;
            return new Vector2(width > size.Y ? width : size.Y, size.Y);
        }
    }
}
