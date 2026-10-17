using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>An angle, given by turning a dial or by typing it into the box beside it, the two kept in step.</summary>
    /// <remarks>
    /// Nought is straight up and positive is clockwise, as the game turns a rotor. It runs to a whole turn either way, because
    /// the game treats a rotor sent to minus ninety and one sent to two hundred and seventy as different instructions.
    /// </remarks>
    public class AngleField : HStack, IField
    {
        const float Turn = 360f;
        const float Notch = 5f;

        readonly Dial _dial;
        readonly NumberBox _box;

        /// <summary>An empty angle held between <paramref name="least" /> and <paramref name="most" /> as written, either of which may be empty for a whole turn.</summary>
        public AngleField(string least, string most, ICommand submit)
        {
            Spacing = 8f;
            var lowest = Bound(least, -Turn);
            var highest = Bound(most, Turn);

            Children.Add(new Dial
            {
                Most = Math.Min(Turn, Math.Max(Math.Abs(lowest), Math.Abs(highest))),
                MarkColor = Palette.Faint,
                QuarterColor = Palette.Muted,
                NeedleColor = Palette.Ink,
                HeldColor = Palette.Live
            }.As(out _dial));

            Children.Add(new NumberBox
            {
                Style = Styles.Value,
                Focusable = true,
                Submit = submit,
                Widest = "-360.0",
                Negative = lowest < 0,
                Step = Notch,
                Least = lowest,
                Most = highest,
                Format = "0.0"
            }.As(out _box));

            _dial.Turned += delegate
            {
                _box.Text = ((int)_dial.Degrees).ToString("0.0", CultureInfo.InvariantCulture);
                _box.MoveCaret(_box.Length, false);
                Raise();
            };

            _box.Edited += delegate
            {
                Reread();
                Raise();
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _box.Text; }
            set
            {
                _box.Text = value ?? "";
                Reread();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _box.Style = value ? Styles.ValueWrong : Styles.Value; }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;

        /// <summary>A bound as written, or <paramref name="none" /> where nothing is written.</summary>
        static float Bound(string written, float none)
        {
            float bound;
            return float.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out bound) ? bound : none;
        }

        /// <summary>Brings the needle into line with what is typed, leaving it alone where what is typed cannot be read yet.</summary>
        void Reread()
        {
            float said;
            if (float.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out said))
            {
                _dial.Degrees = said;
                _dial.Shows = true;
                return;
            }

            if (string.IsNullOrEmpty(_box.Text))
                _dial.Shows = false;
        }

        void Raise()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }
    }
}
