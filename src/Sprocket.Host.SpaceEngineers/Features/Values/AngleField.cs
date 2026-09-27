using System;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>An angle, given by turning a dial or by typing it into the box beside it, the two kept in step.</summary>
    /// <remarks>
    /// Nought is straight up and positive is clockwise, as the game turns a rotor. It counts on past a whole turn, to a whole
    /// turn either way, because the game treats a rotor sent to minus ninety and one sent to two hundred and seventy as
    /// different instructions. Up and down in the box turn it a notch at a time, since left and right move the caret.
    /// </remarks>
    public class AngleField : HStack, IField
    {
        const float Written = 80f;
        const float Turn = 360f;
        const float Notch = 5f;

        readonly Dial _dial;
        readonly TextBox _box;
        readonly float _least;
        readonly float _most;

        /// <summary>An empty angle held between <paramref name="least" /> and <paramref name="most" /> as written, either of which may be empty for a whole turn.</summary>
        public AngleField(string least, string most, ICommand submit)
        {
            Spacing = 8f;
            _least = Bound(least, -Turn);
            _most = Bound(most, Turn);

            Children.Add(new Dial
            {
                Most = Math.Min(Turn, Math.Max(Math.Abs(_least), Math.Abs(_most))),
                MarkColor = Palette.Faint,
                QuarterColor = Palette.Muted,
                NeedleColor = Palette.Ink,
                HeldColor = Palette.Live
            }.As(out _dial));

            Children.Add(new NotchedBox(this)
            {
                Style = Styles.Value,
                Focusable = true,
                Submit = submit,
                Placement = Fixed.Wide(Written)
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

        /// <summary>Turns the angle <paramref name="by" /> notches from what is typed, within its bounds.</summary>
        void Step(int by)
        {
            float degrees;
            if (!float.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out degrees))
                degrees = 0f;

            degrees = (float)Math.Round(degrees / Notch) * Notch + by * Notch;
            degrees = Math.Max(_least, Math.Min(_most, degrees));

            _box.Text = degrees.ToString("0.0", CultureInfo.InvariantCulture);
            _box.MoveCaret(_box.Length, false);
            Reread();
            Raise();
        }

        void Raise()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>The box the angle is typed into, which turns it a notch for each press of up or down.</summary>
        sealed class NotchedBox : TextBox
        {
            readonly AngleField _field;

            public NotchedBox(AngleField field)
            {
                _field = field;
            }

            public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
            {
                if (key == MyKeys.Up || key == MyKeys.Down)
                {
                    _field.Step(key == MyKeys.Up ? 1 : -1);
                    return true;
                }

                return base.OnKey(input, key, shift, ctrl);
            }
        }
    }
}
