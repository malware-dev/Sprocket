using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>One live thing on one line: what it is, what it is doing, and when it last did anything.</summary>
    /// <remarks>
    ///     Whoever holds it sets what it shows and what each press does, then calls <see cref="Show" />, which works
    ///     out its look in one pass. A press it is given no command for is hidden or cannot be pressed. Rows never
    ///     nest more than one level deep, so one fixed indent serves every row.
    /// </remarks>
    public class StatusRow : HStack
    {
        /// <summary>What goes before the words of anything that adds.</summary>
        public const string Plus = "+ ";

        const float Indent = 32f;

        static readonly Fixed ThingHeight = Fixed.High(68f);
        static readonly Fixed ActionHeight = Fixed.High(48f);

        readonly Relay _opens;
        readonly Relay _runs;
        readonly Relay _flips;
        readonly Relay _copies;
        readonly Relay _offers;
        readonly Relay _bins;
        readonly HStack _inside;
        readonly Glyph _light;
        readonly Label _summary;
        readonly Label _status;
        readonly Glyph _run;
        readonly Label _lastRun;
        readonly Label _problem;
        readonly Switch _switch;
        readonly Glyph _duplicate;
        readonly Button _button;
        readonly Glyph _delete;
        readonly HStack _detail;
        readonly Glyph _below;

        string _said;
        bool _saidAsks;

        /// <summary>An empty row, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public StatusRow()
        {
            _opens = new Relay(delegate { Carry(Opens); });
            _runs = new Relay(delegate { Carry(Running ? Stops : Runs); });
            _flips = new Relay(delegate { Carry(Flips); });
            _copies = new Relay(delegate { Carry(Copies); });
            _offers = new Relay(delegate { Carry(Offers); });
            _bins = new Relay(delegate { Carry(Deletes); });

            Style = Styles.Row;
            Command = _opens;
            Children.Add(new HStack
            {
                Spacing = 16f,
                Placement = Flex.Grow,
                Children =
                {
                    new Glyph { Symbol = Symbols.Disc, Style = Styles.Light, Placement = Fixed.Size(16f, 16f) }.As(out _light),
                    new VStack
                    {
                        Spacing = 4f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new HStack
                            {
                                Spacing = 8f,
                                Placement = Fixed.High(32f),
                                Children =
                                {
                                    new Label { Style = Styles.Name, Trims = true }.As(out _summary),
                                    new Glyph { Symbol = Symbols.Warning, Style = Styles.Flag, Placement = Fixed.Size(32f, 32f) }.As(out _below)
                                }
                            },
                            new HStack
                            {
                                Spacing = 8f,
                                Placement = Fixed.High(16f),
                                Children =
                                {
                                    new Label { Style = Styles.Detail }.As(out _status),
                                    new Label { Style = Styles.Detail, Trims = true }.As(out _problem),
                                    new Label { Style = Styles.Detail }.As(out _lastRun)
                                }
                            }.As(out _detail)
                        }
                    },
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Play, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = _runs }.As(out _run),
                            new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = _copies }.As(out _duplicate),
                            new Button { Style = Styles.Button, Command = _offers }.As(out _button),
                            new Switch { Style = Styles.Switch, Command = _flips }.As(out _switch),
                            new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = Fixed.Size(32f, 32f), Command = _bins }.As(out _delete)
                        }
                    }
                }
            }.As(out _inside));
        }

        /// <summary>What pressing the row does, or null for a row that is not pressed.</summary>
        public ICommand Opens { get; set; }

        /// <summary>What starting it does, or null where it cannot be started from here.</summary>
        public ICommand Runs { get; set; }

        /// <summary>What stopping it does, or null where it cannot be stopped from here.</summary>
        public ICommand Stops { get; set; }

        /// <summary>What its switch does, or null for a row with no switch.</summary>
        public ICommand Flips { get; set; }

        /// <summary>What copying it does, or null where it cannot be copied.</summary>
        public ICommand Copies { get; set; }

        /// <summary>What its button does, or null for a row with no button.</summary>
        public ICommand Offers { get; set; }

        /// <summary>What deleting it does, or null where it cannot be deleted.</summary>
        public ICommand Deletes { get; set; }

        /// <summary>How deep it stands under the row it belongs to: none, or one level.</summary>
        public int Depth { get; set; }

        /// <summary>What it is called.</summary>
        public string Summary { get; set; }

        /// <summary>Whether it is a way to add something rather than a thing that is there.</summary>
        public bool IsAction { get; set; }

        /// <summary>Whether pressing it asks something first, which an action row says with an ellipsis.</summary>
        public bool Asks { get; set; }

        /// <summary>What pressing it does, in markdown-lite, said where the pointer rests on it; null for nothing.</summary>
        public string About { get; set; }

        /// <summary>The words for the slots in <see cref="About" />, or null where it has none.</summary>
        public Arguments AboutArguments { get; set; }

        /// <summary>How many things in it are broken.</summary>
        public int Problems { get; set; }

        /// <summary>How many things in it are worth a remark without being broken.</summary>
        public int Remarks { get; set; }

        /// <summary>Whether it is running now.</summary>
        public bool Running { get; set; }

        /// <summary>Whether it is waiting for something before it can go on.</summary>
        public bool Parked { get; set; }

        /// <summary>Whether it is the row the rest of its screen is about.</summary>
        public bool Chosen { get; set; }

        /// <summary>Whether it ran a moment ago.</summary>
        public bool JustRan { get; set; }

        /// <summary>What it is doing, in a word or two, or empty.</summary>
        public string Status { get; set; }

        /// <summary>When it last ran, or empty.</summary>
        public string LastRun { get; set; }

        /// <summary>What is wrong with it, said in place of its status, or empty.</summary>
        public string Problem { get; set; }

        /// <summary>Whether <see cref="Problem" /> is a warning rather than something broken.</summary>
        public bool ProblemIsWarning { get; set; }

        /// <summary>Whether its switch is on.</summary>
        public bool SwitchIsOn { get; set; }

        /// <summary>Whether its switch governs a whole system rather than one thing in it.</summary>
        public bool SwitchGovernsAll { get; set; }

        /// <summary>What copying it does, said on the copy mark.</summary>
        public string DuplicateText { get; set; }

        /// <summary>What its button says.</summary>
        public string ButtonText { get; set; }

        /// <summary>What its button does, in markdown-lite, said where the pointer rests on it.</summary>
        public string ButtonAbout { get; set; }

        /// <summary>The mark that starts or stops it, for a press on it that has to hang something under it.</summary>
        public Element RunPart => _run;

        /// <summary>The switch, for a press on it that has to hang something under it.</summary>
        public Element SwitchPart => _switch;

        /// <summary>The copy mark, for a press on it that has to hang something under it.</summary>
        public Element CopyPart => _duplicate;

        /// <summary>The button, for a press on it that has to hang something under it.</summary>
        public Element ButtonPart => _button;

        /// <summary>The bin, for a press on it that has to hang something under it.</summary>
        public Element DeletePart => _delete;

        /// <summary>What pressing a switch would do, as a note on the switch.</summary>
        public static string Switching(Texts texts, bool governsAll, bool isOn)
        {
            if (!governsAll)
                return isOn ? texts.RowSwitchOff : texts.RowSwitchOn;

            return isOn
                ? texts.RowSwitchSystemOff
                : texts.RowSwitchSystemOn;
        }

        /// <summary>Shows what it has been told, saying its fixed words in <paramref name="texts" />.</summary>
        /// <remarks>Runs on every refresh of the list, so it writes what may have changed and joins no string that has not.</remarks>
        public void Show(Texts texts)
        {
            _inside.Padding = new Thickness(Depth > 0 ? Indent : 0f, 0f, 0f, 0f);

            // One height for every row of a kind, so no row moves the rows under it; one with nothing to say under its
            // name has the name on its middle.
            Placement = IsAction ? ActionHeight : ThingHeight;

            var pressable = Opens != null;
            _opens.CanExecute = pressable;
            Tip = Tip.For(Tip, About, AboutArguments);
            var wrong = Problems > 0;

            Style = wrong
                ? (Chosen ? Styles.BrokenChosen : Styles.Broken)
                : Running
                    ? (Chosen ? Styles.RunningChosen : Styles.Running)
                    : Parked
                        ? (Chosen ? Styles.ParkedChosen : Styles.Parked)
                        : Chosen
                            ? Styles.Chosen
                            : !pressable
                                ? Styles.Still
                                : IsAction
                                    ? Styles.AddRow
                                    : Styles.Row;

            if (!ReferenceEquals(_said, Summary) || _saidAsks != Asks)
            {
                _said = Summary;
                _saidAsks = Asks;

                // The ellipsis is the desktop convention for a button that asks something before it acts.
                _summary.Text = IsAction ? Plus + Summary + (Asks ? "…" : "") : Summary;
            }

            _summary.Color = IsAction ? Palette.Muted : Palette.Ink;

            _light.Tint = wrong
                ? Palette.Problem
                : IsAction
                    ? Palette.None
                    : JustRan
                        ? Palette.Live
                        : Palette.Dim;

            _light.Tip = Tip.For(_light.Tip, IsAction
                ? null
                : wrong
                    ? texts.RowBroken
                    : JustRan
                        ? texts.RowRan
                        : texts.RowIdle);

            _status.Text = Status;
            _lastRun.Text = LastRun;

            var doing = Running ? Stops : Runs;

            _run.Symbol = Running ? Symbols.Stop : Symbols.Play;
            _run.IsVisible = doing != null;
            _runs.CanExecute = doing != null;
            _run.Tip = Tip.For(_run.Tip, doing != null ? (Running ? SequenceText.Stops() : SequenceText.Starts()) : null);

            var saying = !string.IsNullOrEmpty(Problem);

            _detail.IsVisible = saying || !string.IsNullOrEmpty(Status) || !string.IsNullOrEmpty(LastRun);

            _status.IsVisible = !saying;

            _problem.Text = Problem;
            _problem.IsVisible = saying;
            _problem.Color = ProblemIsWarning ? Palette.Notice : Palette.Problem;

            var marked = saying || Problems > 0 || Remarks > 0;
            var quietly = saying ? ProblemIsWarning : Problems == 0;

            _below.IsVisible = marked;
            _below.Tint = quietly ? Palette.Notice : Palette.Problem;

            var switches = Flips != null;
            _switch.IsVisible = switches;
            _switch.IsOn = SwitchIsOn;
            _switch.Says(texts.SwitchOn, texts.SwitchOff);
            _flips.CanExecute = switches;
            _switch.Tip = Tip.For(_switch.Tip, switches ? Switching(texts, SwitchGovernsAll, SwitchIsOn) : null);

            var copies = Copies != null;
            _duplicate.IsVisible = copies;
            _copies.CanExecute = copies;
            _duplicate.Tip = Tip.For(_duplicate.Tip, copies ? DuplicateText : null);

            var offers = Offers != null;
            _button.IsVisible = offers;
            _offers.CanExecute = offers;
            _button.Tip = Tip.For(_button.Tip, offers ? ButtonAbout : null);

            if (offers)
                _button.Text = ButtonText;

            var bins = Deletes != null;
            _delete.IsVisible = bins;
            _bins.CanExecute = bins;
            _delete.Tip = Tip.For(_delete.Tip, bins ? texts.RowDelete : null);
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }
    }
}
