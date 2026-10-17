using System;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A number typed into a box as wide as every other number of the form, held to the kind of number its value is.</summary>
    public class NumberField : HStack, IField
    {
        /// <summary>How wide every number box of a form is.</summary>
        public const float Numbers = 176f;

        readonly NumberBox _box;
        Field _field;

        /// <summary>An empty box for what <paramref name="field" /> asks for, whose Enter carries out <paramref name="submit" />.</summary>
        public NumberField(Field field, ICommand submit) : this(field, submit, Numbers)
        {
        }

        /// <summary>An empty box <paramref name="wide" /> wide for what <paramref name="field" /> asks for, whose Enter carries out <paramref name="submit" />.</summary>
        /// <remarks>The box shows the value as a person reads it and gives it as stored: a value the game holds as 0 to 1 is typed as a percent.</remarks>
        public NumberField(Field field, ICommand submit, float wide)
        {
            _field = field;
            Spacing = 8f;
            double least;
            var floored = Bound(field.Scaled(field.Least), out least);
            double most;
            var ceilinged = Bound(field.Scaled(field.Most), out most);

            Children.Add(new NumberBox
            {
                Style = Styles.Value,
                Focusable = true,
                SelectsAll = true,
                Submit = submit,
                Placement = FixedSize.Wide(wide),
                Whole = field.Entry == FieldInput.Whole,
                Negative = !floored || least < 0,
                Least = floored ? least : (double?)null,
                Most = ceilinged ? most : (double?)null,
                Format = field.Entry == FieldInput.Whole ? "0" : field.Entry == FieldInput.Decimal ? "0.0#" : "0.##"
            }.As(out _box));

            var unit = (field.Unit ?? "").Trim();
            if (unit.Length > 0)
                Children.Add(new Label { Style = Styles.ValueLabel, Text = unit });

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
            get { return _field.Unscaled(_box.Text); }
            set { _box.Text = _field.Scaled(value ?? ""); }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _box.Style = value ? Styles.ValueWrong : Styles.Value; }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;

        /// <summary>Whether a field takes a number: one it says is whole or decimal, or one with a bound that says nothing else.</summary>
        public static bool Takes(Field field)
        {
            if (field.Entry == FieldInput.Whole || field.Entry == FieldInput.Decimal)
                return true;

            return field.Entry == FieldInput.Any && Math.Max((field.Least ?? "").Length, (field.Most ?? "").Length) > 0;
        }

        /// <summary>Whether <paramref name="written" /> is a bound, and which, written as a program writes a number.</summary>
        public static bool Bound(string written, out double bound)
        {
            bound = 0;
            return !string.IsNullOrEmpty(written) && double.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out bound);
        }
    }
}
