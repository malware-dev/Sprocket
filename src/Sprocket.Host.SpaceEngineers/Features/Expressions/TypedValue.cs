using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// A fixed value in an expression being built, set where it stands: a number, a text or a timespan typed into a box as
    /// wide as what is typed, on or off by a switch.
    /// </summary>
    public sealed class TypedValue : HStack
    {
        readonly ValueKind _kind;
        readonly TextBox _box;
        readonly Switch _switch;

        /// <summary>A box or switch set to <paramref name="value" />, its words in <paramref name="texts" />.</summary>
        public TypedValue(Texts texts, Literal value)
        {
            _kind = value.Kind;
            Base.Spacing = Gaps.Parts;
            if (_kind == ValueKind.Flag)
            {
                Children.Add(new Switch { Style = Styles.Switch, IsOn = value.Flag, OnText = texts.SwitchOn, OffText = texts.SwitchOff, Align = Align.Center }.As(out _switch));
                _switch.Command = new RelayCommand(() =>
                {
                    _switch.IsOn = !_switch.IsOn;
                    Typed?.Invoke(new Operand(Literal.Of(_switch.IsOn)));
                });
                return;
            }

            Children.Add(new TextBox
            {
                Style = Styles.Value,
                Focusable = true,
                Text = Shown(value),
                Align = Align.Center
            }.As(out _box));
            _box.Edited += Read;
            if (_kind == ValueKind.Duration)
                Children.Add(new Label { Style = Styles.Detail, Text = texts.WaitInSeconds, Align = Align.Center });
        }

        /// <summary>Raised with the value as it is set, each time what is typed reads as one.</summary>
        public event Action<Expression> Typed;

        /// <summary>The box the value is typed in, or null for on or off.</summary>
        public TextBox Box => _box;

        /// <summary>The switch on or off is set by, or null for a value typed in.</summary>
        public Switch Switch => _switch;

        /// <summary>A neutral value of <paramref name="kind" /> to start from: nothing, no time, an empty text or off.</summary>
        public static Literal Neutral(KindClass kind)
        {
            switch (kind)
            {
                case KindClass.Duration: return Literal.Of(Duration.OfSeconds(0));
                case KindClass.Text: return Literal.Of("");
                case KindClass.Flag: return Literal.Of(false);
            }

            return Literal.Of(0L);
        }

        /// <summary>Whether <paramref name="atom" /> is a fixed value set where it stands, rather than named.</summary>
        public static bool Takes(Expression atom)
        {
            var operand = atom as Operand;
            if (operand == null || operand.Value.IsSetting)
                return false;

            switch (operand.Value.Kind)
            {
                case ValueKind.Integer:
                case ValueKind.Real:
                case ValueKind.Text:
                case ValueKind.Duration:
                case ValueKind.Flag:
                    return true;
            }

            return false;
        }

        // What the box starts with: a length in seconds, without its unit, which stands beside the box.
        static string Shown(Literal value)
        {
            if (value.Kind != ValueKind.Duration)
                return value.Said;

            var length = value.Length;
            var seconds = length.InSeconds ? length.Seconds : length.Ticks / 60d;
            return seconds.ToString("R", CultureInfo.InvariantCulture);
        }

        // Says the value typed, where it reads as one of its kind.
        void Read()
        {
            var said = _box.Text;
            if (_kind == ValueKind.Text)
            {
                _box.Set(LookStates.Wrong, false);
                Typed?.Invoke(new Operand(Literal.Of(said)));
                return;
            }

            double number;
            var read = double.TryParse(said.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                && !double.IsNaN(number) && !double.IsInfinity(number);
            _box.Set(LookStates.Wrong, !read);
            if (!read)
                return;

            Literal value;
            if (_kind == ValueKind.Duration)
                value = Literal.Of(Duration.OfSeconds(number));
            else if (number == Math.Floor(number) && Math.Abs(number) < long.MaxValue && said.IndexOfAny(new[] { '.', ',', 'e', 'E' }) < 0)
                value = Literal.Of((long)number);
            else
                value = Literal.Of(number);

            Typed?.Invoke(new Operand(value));
        }
    }
}
