using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>A length of time given in a form: a number typed into a box as wide as the form's other numbers, in seconds or in ticks.</summary>
    /// <remarks>A change of unit keeps the length and says it in the other unit: 2 seconds becomes 120 ticks.</remarks>
    public class DurationField : HStack, IField
    {
        const string SecondsFormat = "0.##";

        readonly NumberBox _box;
        readonly Switch _unit;
        bool _inSeconds = true;

        /// <summary>An empty box in seconds, its unit switched between <paramref name="texts" />' words for seconds and ticks, whose Enter carries out <paramref name="submit" />.</summary>
        public DurationField(Texts texts, ICommand submit)
        {
            Base.Spacing = Gaps.Parts;
            Children.Add(new NumberBox
            {
                Style = Styles.Number,
                Focusable = true,
                SelectsAll = true,
                Submit = submit,
                Placement = FixedSize.Width(NumberField.Numbers),
                Minimum = 0
            }.As(out _box));
            Children.Add(new Switch { Style = Styles.Switch }.As(out _unit));

            _unit.OnText = texts.WaitInSeconds;
            _unit.OffText = texts.WaitInTicks;
            _unit.IsOn = true;
            _unit.Command = new RelayCommand(Flip);
            _box.Edited += Told;
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <summary>The length as a program writes it, such as <c>2s</c> or <c>60t</c>; empty where the box holds no number.</summary>
        public string Text
        {
            get
            {
                double number;
                if (!double.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    return "";

                return _inSeconds
                    ? Literal.Of(Duration.OfSeconds(number)).Said
                    : Literal.Of(Duration.OfTicks((int)Math.Round(number))).Said;
            }
            set
            {
                Duration length;
                if (!Read(value, out length))
                {
                    _box.Text = "";
                    return;
                }

                Unit(length.InSeconds);
                _box.Text = length.InSeconds ? length.Seconds.ToString(SecondsFormat, CultureInfo.InvariantCulture) : length.Ticks.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Whether <paramref name="written" /> is a length as a program writes one, such as <c>2s</c> or <c>60t</c>, and which.</summary>
        public static bool Read(string written, out Duration length)
        {
            length = Duration.OfTicks(0);
            if (string.IsNullOrEmpty(written) || written.Length < 2)
                return false;

            var unit = written[written.Length - 1];
            double number;
            if ((unit != 's' && unit != 't') || !double.TryParse(written.Substring(0, written.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || number < 0)
                return false;

            length = unit == 's' ? Duration.OfSeconds(number) : Duration.OfTicks((int)Math.Round(number));
            return true;
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _box.Set(LookStates.Wrong, value); }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;

        /// <summary>Says the length in the other unit, keeping it.</summary>
        void Flip()
        {
            double number;
            var has = double.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
            Unit(!_inSeconds);
            if (has)
                _box.Text = _inSeconds
                    ? (number / Duration.PerSecond).ToString(SecondsFormat, CultureInfo.InvariantCulture)
                    : Duration.TicksIn(number).ToString(CultureInfo.InvariantCulture);
            Told();
        }

        /// <summary>Shows <paramref name="seconds" /> as the unit, an integer count of ticks or seconds to two places.</summary>
        void Unit(bool seconds)
        {
            _inSeconds = seconds;
            _unit.IsOn = seconds;
            _box.IsInteger = !seconds;
        }

        void Told()
        {
            Edited?.Invoke();
        }
    }
}
