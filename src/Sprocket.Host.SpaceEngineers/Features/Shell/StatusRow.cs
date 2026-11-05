using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Input;
using Lumel.Text;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>One live thing on one line: what it is, what it is doing, and when it last did anything.</summary>
    public class StatusRow : HStack
    {
        /// <summary>What goes before the words of anything that adds.</summary>
        public const string Plus = "+ ";

        static readonly FixedSize ThingHeight = FixedSize.Height(68f);

        readonly RelayCommand _opens;
        readonly RelayCommand _runs;
        readonly RelayCommand _flips;
        readonly RelayCommand _copies;
        readonly RelayCommand _offers;
        readonly RelayCommand _bins;
        // As wide as a symbol is drawn, so the gap beside a marked row's name is the row's own.
        const float MarkWidth = 20f;

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

        /// <summary>An empty row, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public StatusRow()
        {
            _opens = new RelayCommand(() => Carry(Opens));
            _runs = new RelayCommand(() => Carry(Running ? Stops : Runs));
            _flips = new RelayCommand(() => Carry(Flips));
            _copies = new RelayCommand(() => Carry(Copies));
            _offers = new RelayCommand(() => Carry(Offers));
            _bins = new RelayCommand(() => Carry(Deletes));

            Style = Styles.Row;
            Command = _opens;
            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Items },
                Placement = Flex.Grow,
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Disc, Style = Styles.Light, Placement = FixedSize.Size(16f, 16f) }.As(out _light),
                            new VStack
                            {
                                Base = { Spacing = Gaps.Within },
                                Placement = Flex.Grow,
                                Children =
                                {
                                    new HStack
                                    {
                                        Base = { Spacing = Gaps.Parts },
                                        Placement = FixedSize.Height(32f),
                                        Children =
                                        {
                                            new Label { Style = Styles.Name, TextWrapping = TextWrapping.Trim }.As(out _summary),
                                            new Glyph { Symbol = Symbols.Warning, Style = Styles.Flag, Placement = FixedSize.Size(32f, 32f) }.As(out _below)
                                        }
                                    },
                                    new HStack
                                    {
                                        Base = { Spacing = Gaps.Parts },
                                        Placement = FixedSize.Height(16f),
                                        Children =
                                        {
                                            new Label { Style = Styles.Detail }.As(out _status),
                                            new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Trim }.As(out _problem),
                                            new Label { Style = Styles.Detail }.As(out _lastRun)
                                        }
                                    }.As(out _detail)
                                }
                            }
                        }
                    },
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Play, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _runs }.As(out _run),
                            new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = _copies }.As(out _duplicate),
                            new Button { Style = Styles.Button, Command = _offers }.As(out _button),
                            new Switch { Style = Styles.SystemSwitch, Command = _flips }.As(out _switch),
                            new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f), Command = _bins }.As(out _delete)
                        }
                    }
                }
            });
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
        /// <remarks>Whatever holds it sets it in; a row is as wide as it is given.</remarks>
        public int Depth { get; set; }

        /// <summary>The symbol its light is drawn as, or null for the round light every row has.</summary>
        public string Mark { get; set; }

        /// <summary>The symbol its bin is drawn as, or null for the bin.</summary>
        public string BinMark { get; set; }

        /// <summary>What it is called.</summary>
        public string Summary { get; set; }

        /// <summary>What pressing it does, in markdown-lite, said where the pointer rests on it; null for nothing.</summary>
        public string About { get; set; }

        /// <summary>The words for the slots in <see cref="About" />, or null where it has none.</summary>
        public Arguments AboutArguments { get; set; }

        /// <summary>How many things in it are broken.</summary>
        public int Problems { get; set; }

        /// <summary>How many things in it are worth a remark without being broken.</summary>
        public int Remarks { get; set; }

        /// <summary>Whether it is the one chosen from its list, whose details stand beside the list.</summary>
        public bool Chosen { get; set; }

        /// <summary>Whether it is running now.</summary>
        public bool Running { get; set; }

        /// <summary>Whether it is waiting for something before it can go on.</summary>
        public bool Parked { get; set; }

        /// <summary>Whether it ran a moment ago.</summary>
        public bool JustRan { get; set; }

        /// <summary>What it is doing, in a word or two, or empty.</summary>
        public string Status { get; set; }

        /// <summary>When it last ran, or empty.</summary>
        public string LastRun { get; set; }

        /// <summary>What is wrong with it, or worth a word, said in place of its status, or empty.</summary>
        public string Problem { get; set; }

        /// <summary>How much <see cref="Problem" /> matters: a problem, a warning or a note.</summary>
        public NoticeLevel Level { get; set; } = NoticeLevel.Problem;

        /// <summary>Whether its switch is on.</summary>
        public bool SwitchIsOn { get; set; }

        /// <summary>Whether its switch governs a whole system rather than one thing in it.</summary>
        public bool SwitchGovernsAll { get; set; }

        /// <summary>What copying it does, said on the copy mark.</summary>
        public string DuplicateText { get; set; }

        /// <summary>What deleting it does, said on the bin.</summary>
        public string DeleteText { get; set; }

        /// <summary>Why it cannot be deleted now, said on its quiet bin; empty where nothing stands in the way.</summary>
        public string Locked { get; set; }

        /// <summary>Why its switch cannot be pressed now, said on the switch; empty where nothing stands in the way.</summary>
        public string SwitchLocked { get; set; }

        /// <summary>What pressing its switch does, said on it, or null for the plain words.</summary>
        public string SwitchAbout { get; set; }

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
                return isOn ? texts.Codex.RowSwitchOff : texts.Codex.RowSwitchOn;

            return isOn
                ? texts.Codex.RowSwitchSystemOff
                : texts.Codex.RowSwitchSystemOn;
        }

        /// <summary>Shows what it has been told, saying its fixed words in <paramref name="texts" />.</summary>
        /// <remarks>Runs on every refresh of the list, so it joins no string that has not changed.</remarks>
        public void Show(Texts texts)
        {
            _summary.Style = Depth > 0 ? Styles.InnerName : Styles.Name;
            _light.Symbol = Mark ?? Symbols.Disc;
            _light.Style = Mark == null ? Styles.Light : Styles.Marked;
            _light.Placement = Mark == null ? FixedSize.Size(16f, 16f) : FixedSize.Size(MarkWidth, 16f);

            Placement = ThingHeight;

            var pressable = Opens != null;
            _opens.CanExecute = pressable;
            Tip = CodexTip.For(Tip, About, AboutArguments);
            var wrong = Problems > 0;

            // A row is off where its own switch is, or where the logic system above it holds its switch.
            var off = Flips != null && (!SwitchIsOn || !string.IsNullOrEmpty(SwitchLocked));

            var warned = Remarks > 0 || !string.IsNullOrEmpty(Problem) && Level == NoticeLevel.Warning || Parked || off;
            Style = wrong
                ? Chosen ? Styles.BrokenChosen : Styles.Broken
                : warned
                    ? Chosen ? Styles.ParkedChosen : Styles.Parked
                    : Chosen
                        ? Styles.Chosen
                        : Running
                            ? Styles.Running
                            : !pressable
                                ? Styles.Still
                                : Styles.Row;

            if (!ReferenceEquals(_said, Summary))
            {
                _said = Summary;
                _summary.Text = Summary;
            }

            _light.Base.Tint = wrong
                ? Palette.Problem
                : JustRan
                    ? Palette.Live
                    : Mark == null ? Palette.Dim : Palette.Muted;

            _light.Tip = wrong
                ? KeptTip.For(_light.Tip, texts.Codex.RowBroken)
                : CodexTip.For(_light.Tip, JustRan ? texts.Codex.RowRan : texts.Codex.RowIdle);

            _status.Text = Status;
            _lastRun.Text = LastRun;

            var doing = Running ? Stops : Runs;

            _run.Symbol = Running ? Symbols.Stop : Symbols.Play;
            _run.IsVisible = doing != null;
            _runs.CanExecute = doing != null;
            _run.Tip = KeptTip.For(_run.Tip, doing != null ? (Running ? SequenceText.Stops(texts) : SequenceText.Starts(texts)) : null);

            var saying = !string.IsNullOrEmpty(Problem);

            _detail.IsVisible = saying || !string.IsNullOrEmpty(Status) || !string.IsNullOrEmpty(LastRun);

            _status.IsVisible = !saying;

            _problem.Text = Problem;
            _problem.IsVisible = saying;
            _problem.Base.Color = RemarkLook.Of(Level, Palette.Faint);

            var marked = saying && Level != NoticeLevel.Info || Problems > 0 || Remarks > 0;
            var quietly = saying && Level != NoticeLevel.Info ? Level == NoticeLevel.Warning : Problems == 0;

            _below.IsVisible = marked;
            _below.Base.Tint = quietly ? Palette.Notice : Palette.Problem;

            var switches = Flips != null;
            var held = !string.IsNullOrEmpty(SwitchLocked);
            _switch.IsVisible = switches;
            _switch.IsOn = SwitchIsOn;
            _switch.OnText = texts.SwitchOn;
            _switch.OffText = texts.SwitchOff;
            _flips.CanExecute = switches && !held;
            _switch.Tip = switches && held
                ? KeptTip.For(_switch.Tip, SwitchLocked)
                : CodexTip.For(_switch.Tip, !switches ? null : SwitchAbout ?? Switching(texts, SwitchGovernsAll, SwitchIsOn));

            var locked = !string.IsNullOrEmpty(Locked);
            var copies = Copies != null;
            _duplicate.IsVisible = copies || locked && DuplicateText != null;
            _copies.CanExecute = copies;
            _duplicate.Tip = KeptTip.For(_duplicate.Tip, copies ? DuplicateText : locked ? Locked : null);

            var offers = Offers != null;
            _button.IsVisible = offers;
            _offers.CanExecute = offers;
            _button.Tip = CodexTip.For(_button.Tip, offers ? ButtonAbout : null);

            if (offers)
                _button.Text = ButtonText;

            var bins = Deletes != null;
            _delete.IsVisible = bins || locked;
            _delete.Symbol = BinMark ?? Symbols.Delete;
            _bins.CanExecute = bins;
            _delete.Tip = KeptTip.For(_delete.Tip, bins ? DeleteText : locked ? Locked : null);
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }
    }
}
