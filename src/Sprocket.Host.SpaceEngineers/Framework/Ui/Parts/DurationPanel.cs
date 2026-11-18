using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>A length of time given in a panel: a number stepped or typed, in seconds or in ticks, the length in the other unit beneath it, and a few lengths to pick.</summary>
    /// <remarks>Enter and Escape are said rather than acted on, since what closing means is the holder's.</remarks>
    public class DurationPanel : VStack
    {
        const double SecondsStep = 0.5;
        const int TicksStep = 10;
        // The widest number in either unit, so the panel holds its size as the unit changes.
        const string Widest = "0000.00";
        const string SecondsFormat = "0.##";
        const int CostlyTicks = 10;

        // The same in both units, so the picks of either line up: room for "1800 t".
        const float PickWidth = 64f;

        // In seconds, and offered in either unit, so the two rows say the same lengths.
        static readonly double[] Picks = { 0.5, 1, 5, 10, 30, 60 };

        readonly Texts _texts;
        readonly InputRoot _input;
        readonly NumberBox _box;
        readonly Switch _unit;
        readonly Label _other;
        readonly Label _caution;
        readonly Deck _picks;
        readonly HStack _secondsPicks;
        readonly HStack _ticksPicks;
        readonly SpinBox _spin;
        readonly HStack _spacer;
        readonly SettingChip _chip;
        readonly Glyph _mark;

        string _setting = "";
        bool _inSeconds;
        bool _cautions;

        /// <summary>A panel showing <paramref name="start" /> in the unit it is written in, whose box keeps the keyboard of <paramref name="input" /> whatever in it is pressed.</summary>
        public DurationPanel(Texts texts, InputRoot input, Duration start)
        {
            _texts = texts;
            _input = input;
            Base.Spacing = Gaps.Parts;

            var number = new HStack { Base = { Spacing = Gaps.Parts } };
            _spin = new SpinBox
            {
                Style = Styles.Spin,
                LessTip = KeptTip.For(null, texts.Codex.WaitShorter),
                MoreTip = KeptTip.For(null, texts.Codex.WaitLonger)
            };
            _box = _spin.Box;
            _box.Focusable = true;
            _box.Minimum = 0;
            _box.Widest = Widest;
            _box.Submit = new RelayCommand(() => Raise(Submitted));
            _box.Tip = CodexTip.For(null, texts.Codex.SequenceWaitTypedAbout);
            number.Children.Add(_spin);
            number.Children.Add(new SettingChip { Placement = Flex.Grow, IsVisible = false }.As(out _chip));
            // At the right end, across from the number it is the unit of.
            number.Children.Add(new HStack { Placement = Flex.Spacer }.As(out _spacer));
            number.Children.Add(new Switch
            {
                Style = Styles.Switch,
                Tip = CodexTip.For(null, texts.Codex.TermTick),
                Command = new RelayCommand(() => Unit(!_inSeconds))
            }.As(out _unit));
            _unit.OnText = texts.WaitInSeconds;
            _unit.OffText = texts.WaitInTicks;
            number.Children.Add(new Glyph
            {
                Style = Styles.Source,
                Symbol = Symbols.Link,
                IsVisible = false,
                Tip = CodexTip.For(null, texts.ValueSourceAbout),
                Command = new RelayCommand(() => Sourcing?.Invoke(_mark))
            }.As(out _mark));
            Children.Add(number);

            Children.Add(new Label { Style = Styles.WarningLine, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _caution));

            _secondsPicks = new HStack { Base = { Spacing = Gaps.Parts } };
            foreach (var seconds in Picks)
            {
                _secondsPicks.Children.Add(new Button
                {
                    Style = Styles.LengthPick,
                    Placement = FixedSize.Width(PickWidth),
                    Text = seconds == 60 ? texts.WaitPickMinute : Template.Fill(texts.WaitPickSeconds, "count", Said(seconds)),
                    Command = new RelayCommand(() => Give(Said(seconds)))
                });
            }

            _ticksPicks = new HStack { Base = { Spacing = Gaps.Parts } };
            foreach (var seconds in Picks)
            {
                var ticks = Duration.OfSeconds(seconds).Ticks;
                _ticksPicks.Children.Add(new Button
                {
                    Style = Styles.LengthPick,
                    Placement = FixedSize.Width(PickWidth),
                    Text = Template.Fill(texts.WaitPickTicks, "count", Said(ticks)),
                    Command = new RelayCommand(() => Give(Said(ticks)))
                });
            }

            // One over the other, so the panel holds its size whichever unit shows.
            Children.Add(new Deck { Children = { _secondsPicks, _ticksPicks } }.As(out _picks));

            // As tall as a button, so a Done the holder lays over its right end sits on its line: 6 + 20 + 6 = 32.
            Children.Add(new Label { Base = { Padding = new Thickness(0f, 6f) }, Style = Styles.Aside }.As(out _other));

            _box.Edited += Show;

            Shape(start.InSeconds);
            _box.Text = start.InSeconds ? Said(start.Seconds) : Said(start.Ticks);
            Show();
        }

        /// <summary>Raised with the mark when where the length comes from is to be chosen: a length of its own or a setting.</summary>
        public event Action<Element> Sourcing;

        /// <summary>Whether the length may come from a setting, which gives it a mark to choose that by.</summary>
        public bool Sourced
        {
            get { return _mark.IsVisible; }
            set { _mark.IsVisible = value; }
        }

        /// <summary>The setting giving the length, or empty where the length is its own.</summary>
        public string Setting => _setting;

        /// <summary>The mark choosing where the length comes from.</summary>
        public Glyph Mark => _mark;

        /// <summary>Has the setting <paramref name="setting" /> give the length, its default said as <paramref name="said" />, or the length its own again where <paramref name="setting" /> is empty.</summary>
        public void Choose(string setting, string said)
        {
            _setting = setting ?? "";
            _chip.Setting = _setting;
            _chip.Default = string.IsNullOrEmpty(said) ? "" : Template.Fill(_texts.ValueByDefault, "value", said);
            Show();
        }

        /// <summary>Raised when the unit is chosen, with whether it is seconds.</summary>
        public event Action<bool> Switched;

        /// <summary>Raised when Enter is pressed in the box.</summary>
        public event Action Submitted;

        /// <summary>Raised when Escape is pressed anywhere in the panel.</summary>
        public event Action Cancelled;

        /// <summary>The box the number is typed into, which takes the keyboard when the panel opens.</summary>
        public NumberBox Box => _box;

        /// <summary>Whether the number is in seconds rather than ticks.</summary>
        public bool InSeconds => _inSeconds;

        /// <summary>Whether it says what a length costs as a rate, where it is short enough to be costly.</summary>
        public bool Cautions
        {
            get { return _cautions; }
            set
            {
                _cautions = value;
                Show();
            }
        }

        /// <summary>The length given, or null where the box holds nothing that reads as one.</summary>
        public Duration? Length
        {
            get
            {
                if (_inSeconds)
                {
                    double seconds;
                    return double.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds)
                           && seconds * Duration.PerSecond <= int.MaxValue
                        ? Duration.OfSeconds(seconds)
                        : (Duration?)null;
                }

                int ticks;
                return int.TryParse(_box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out ticks)
                    ? Duration.OfTicks(ticks)
                    : (Duration?)null;
            }
        }

        /// <inheritdoc />
        /// <remarks>Escape is said as <see cref="Cancelled" />, whatever in the panel holds the keyboard, where anything listens.</remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Escape || Cancelled == null)
                return base.OnKeyDown(input, key, shift, ctrl);

            Raise(Cancelled);
            return true;
        }

        /// <summary>Gives the keyboard back to the box, which a press on anything else in the panel took from it.</summary>
        void Keep()
        {
            if (_input != null)
                _input.Focus(_box);
        }

        /// <summary>Puts the length in <paramref name="seconds" /> or in ticks, the number turned into the new unit.</summary>
        void Unit(bool seconds)
        {
            Keep();
            if (seconds == _inSeconds)
                return;

            var length = Length;
            Shape(seconds);
            if (length.HasValue)
                _box.Text = seconds ? Said((double)length.Value.Ticks / Duration.PerSecond) : Said(length.Value.Ticks);

            _box.SelectAll();
            Show();

            if (Switched != null)
                Switched(seconds);
        }

        /// <summary>Sets the box, the choice of unit and the lengths to pick for <paramref name="seconds" /> or ticks.</summary>
        void Shape(bool seconds)
        {
            _inSeconds = seconds;
            _box.IsInteger = !seconds;
            _box.SmallChange = seconds ? SecondsStep : TicksStep;
            _box.FormatString = seconds ? SecondsFormat : "0";
            _unit.IsOn = seconds;
            _picks.Top = seconds ? _secondsPicks : _ticksPicks;
        }

        /// <summary>Puts <paramref name="said" /> in the box, as an edit.</summary>
        void Give(string said)
        {
            Keep();
            _box.SelectAll();
            _box.Insert(said);
        }

        /// <summary>Says the length in the other unit and what it costs as a rate, and marks the length to pick that it is.</summary>
        void Show()
        {
            var own = _setting.Length == 0;
            _spin.IsVisible = own;
            _spacer.IsVisible = own;
            _unit.IsVisible = own;
            _picks.IsVisible = own;
            _chip.IsVisible = !own;
            _mark.Set(LookStates.Chosen, !own);

            var length = Length;
            _other.Text = own && length.HasValue ? TimeText.Other(_texts, length.Value) : "";

            var costly = own && _cautions && length.HasValue && length.Value.Ticks <= CostlyTicks;
            _caution.IsVisible = costly;
            if (costly)
                _caution.Text = length.Value.Ticks <= 1 ? _texts.RateEveryTickCost : _texts.RateFastCost;

            var picks = _inSeconds ? _secondsPicks : _ticksPicks;
            for (var i = 0; i < picks.Children.Count; i++)
            {
                var given = length.HasValue && length.Value.Ticks == Duration.OfSeconds(Picks[i]).Ticks;
                picks.Children[i].Set(LookStates.Chosen, given);
            }
        }

        static string Said(double number)
        {
            return number.ToString(SecondsFormat, CultureInfo.InvariantCulture);
        }

        static string Said(int number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        static void Raise(Action happened)
        {
            if (happened != null)
                happened();
        }
    }
}
