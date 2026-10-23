using System;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
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

        // The same in both units, so the two rows of seven line up: room for "0.5 s".
        const float PickWidth = 68f;

        static readonly double[] SecondsPicks = { 0.5, 1, 2, 5, 10, 30, 60 };
        static readonly int[] TicksPicks = { 1, 10, 30, 60, 100, 300, 600 };

        readonly Texts _texts;
        readonly InputRoot _input;
        readonly NumberBox _box;
        readonly Switch _unit;
        readonly Label _other;
        readonly Label _caution;
        readonly Deck _picks;
        readonly HStack _secondsPicks;
        readonly HStack _ticksPicks;

        bool _inSeconds;
        bool _cautions;

        /// <summary>A panel showing <paramref name="start" /> in the unit it is written in, whose box keeps the keyboard of <paramref name="input" /> whatever in it is pressed.</summary>
        public DurationPanel(Texts texts, InputRoot input, Duration start)
        {
            _texts = texts;
            _input = input;
            Spacing = 12f;

            var number = new HStack { Spacing = 8f };
            number.Children.Add(new Glyph
            {
                Symbol = Symbols.Minus,
                Style = Styles.Icon,
                Placement = FixedSize.Size(32f, 32f),
                Command = new Relay(() => Step(-1)),
                Tip = Tip.For(null, texts.Codex.WaitShorter)
            });
            number.Children.Add(new NumberBox
            {
                Style = Styles.Value,
                Focusable = true,
                Least = 0,
                Widest = Widest,
                Submit = new Relay(() => Raise(Submitted)),
                Tip = Tip.For(null, texts.Codex.SequenceWaitTypedAbout)
            }.As(out _box));
            number.Children.Add(new Glyph
            {
                Symbol = Symbols.Plus,
                Style = Styles.Icon,
                Placement = FixedSize.Size(32f, 32f),
                Command = new Relay(() => Step(1)),
                Tip = Tip.For(null, texts.Codex.WaitLonger)
            });
            // At the right end, across from the number it is the unit of.
            number.Children.Add(new HStack { Placement = Flex.Spare });
            number.Children.Add(new Switch
            {
                Style = Styles.Switch,
                Tip = Tip.For(null, texts.Codex.WaitTickAbout),
                Command = new Relay(() => Unit(!_inSeconds))
            }.As(out _unit));
            _unit.Says(texts.WaitInSeconds, texts.WaitInTicks);
            Children.Add(number);

            Children.Add(new Label { Style = Styles.Caution, Wraps = true, IsVisible = false }.As(out _caution));

            _secondsPicks = new HStack { Spacing = 6f };
            foreach (var seconds in SecondsPicks)
            {
                _secondsPicks.Children.Add(new Button
                {
                    Style = Styles.Word,
                    Placement = FixedSize.Wide(PickWidth),
                    Text = seconds == 60 ? texts.WaitPickMinute : Template.Fill(texts.WaitPickSeconds, "count", Said(seconds)),
                    Command = new Relay(() => Give(Said(seconds)))
                });
            }

            _ticksPicks = new HStack { Spacing = 6f };
            foreach (var ticks in TicksPicks)
            {
                _ticksPicks.Children.Add(new Button
                {
                    Style = Styles.Word,
                    Placement = FixedSize.Wide(PickWidth),
                    Text = Template.Fill(texts.WaitPickTicks, "count", Said(ticks)),
                    Command = new Relay(() => Give(Said(ticks)))
                });
            }

            // One over the other, so the panel is as wide as the wider of the two rows whichever unit shows.
            Children.Add(new Deck { Children = { _secondsPicks, _ticksPicks } }.As(out _picks));

            // As tall as a button, so a Done the holder lays over its right end sits on its line: 6 + 20 + 6 = 32.
            Children.Add(new Label { Style = Styles.Aside, Padding = new Thickness(0f, 6f) }.As(out _other));

            _box.Edited += Show;

            Shape(start.InSeconds);
            _box.Text = start.InSeconds ? Said(start.Seconds) : Said(start.Ticks);
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
        protected override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Escape || Cancelled == null)
                return base.OnKey(input, key, shift, ctrl);

            Raise(Cancelled);
            return true;
        }

        /// <summary>Moves the number <paramref name="by" /> steps, the keyboard kept in the box.</summary>
        void Step(int by)
        {
            Keep();
            _box.StepBy(by);
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
            _box.Whole = !seconds;
            _box.Step = seconds ? SecondsStep : TicksStep;
            _box.Format = seconds ? SecondsFormat : "0";
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
            var length = Length;
            _other.Text = length.HasValue ? TimeText.Other(_texts, length.Value) : "";

            var costly = _cautions && length.HasValue && length.Value.Ticks <= CostlyTicks;
            _caution.IsVisible = costly;
            if (costly)
                _caution.Text = length.Value.Ticks <= 1 ? _texts.RateEveryTickCost : _texts.RateFastCost;

            var picks = _inSeconds ? _secondsPicks : _ticksPicks;
            for (var i = 0; i < picks.Children.Count; i++)
            {
                var given = length.HasValue && (_inSeconds ? length.Value.Seconds == SecondsPicks[i] : length.Value.Ticks == TicksPicks[i]);
                ((Button)picks.Children[i]).Style = given ? Styles.WordChosen : Styles.Word;
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
