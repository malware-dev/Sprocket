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
    /// <remarks>Every number box of a form is one width, so the boxes start and end together.</remarks>
    public class NumberField : HStack, IField
    {
        /// <summary>How wide every number box of a form is.</summary>
        public const float Numbers = 176f;

        readonly NumberBox _box;

        /// <summary>An empty box for what <paramref name="field" /> asks for, whose Enter carries out <paramref name="submit" />.</summary>
        public NumberField(Field field, ICommand submit)
        {
            double least;
            var floored = Bound(field.Least, out least);
            double most;
            var ceilinged = Bound(field.Most, out most);

            Children.Add(new NumberBox
            {
                Style = Styles.Value,
                Focusable = true,
                Submit = submit,
                Placement = Fixed.Wide(Numbers),
                Whole = field.Entry == Entry.Whole,
                Negative = !floored || least < 0,
                Least = floored ? least : (double?)null,
                Most = ceilinged ? most : (double?)null,
                Format = field.Entry == Entry.Whole ? "0" : field.Entry == Entry.Decimal ? "0.0#" : "0.##"
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

        /// <summary>Whether a field takes a number: one it says is whole or decimal, or one with a bound that says nothing else.</summary>
        public static bool Takes(Field field)
        {
            if (field.Entry == Entry.Whole || field.Entry == Entry.Decimal)
                return true;

            return field.Entry == Entry.Any && Math.Max((field.Least ?? "").Length, (field.Most ?? "").Length) > 0;
        }

        static bool Bound(string written, out double bound)
        {
            bound = 0;
            return !string.IsNullOrEmpty(written) && double.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out bound);
        }
    }
}
