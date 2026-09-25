using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>One live thing on one line: what it is, what it is doing, and when it last did anything.</summary>
    /// <remarks>Core never nests entries more than one level deep, so one fixed indent serves every row.</remarks>
    public sealed class StatusRow
    {
        const float Inset = 32f;

        readonly ComponentView _view;
        readonly Relay _open;
        readonly Relay _runs;
        readonly Relay _changes;
        readonly Relay _flips;
        readonly Relay _copies;
        readonly Relay _offers;
        readonly Relay _bins;
        readonly HStack _line;
        readonly HStack _inside;
        readonly Glyph _light;
        readonly Label _summary;
        readonly Button _status;
        readonly Glyph _run;
        readonly Label _lastRun;
        readonly Label _problem;
        readonly Switch _switch;
        readonly Glyph _duplicate;
        readonly Button _button;
        readonly Glyph _delete;
        readonly HStack _detail;
        readonly Glyph _below;
        string _opens;
        string _opensSays;
        StatusEntry _entry;

        /// <summary>What the light says for each thing it can show.</summary>
        readonly Document _broken = Markdown.Parse("**Something needs mending**\nOpen it to see what.");
        readonly Document _ran = Markdown.Parse("**Ran just now**");
        readonly Document _idle = Markdown.Parse("**Idle**\nLights green when it runs, and red when something in it needs mending.");

        /// <summary>An empty row belonging to <paramref name="view" />, filled in by <see cref="Update" />.</summary>
        public StatusRow(ComponentView view)
        {
            _view = view;
            _open = new Relay(Open);
            _runs = new Relay(Run);
            _changes = new Relay(Change);
            _flips = new Relay(Flip);
            _copies = new Relay(Copy);
            _offers = new Relay(Offer);
            _bins = new Relay(Bin);

            _line = new HStack
            {
                Style = Styles.Row,
                Command = _open,
                Children =
                {
                    new HStack
                    {
                        Spacing = 16f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Disc, Style = Styles.Light }.As(out _light),
                            new VStack
                            {
                                Spacing = 4f,
                                Placement = Flex.Grow,
                                Children =
                                {
                                    new HStack
                                    {
                                        Spacing = 8f,
                                        Children =
                                        {
                                            new Label { Style = Styles.Name, Wraps = true }.As(out _summary),
                                            new Glyph { Symbol = Symbols.Warning, Style = Styles.Flag }.As(out _below)
                                        }
                                    },
                                    new HStack
                                    {
                                        Spacing = 8f,
                                        Children =
                                        {
                                            new Button { Style = Styles.Status, Command = _changes }.As(out _status),
                                            new Label { Style = Styles.Detail, Wraps = true }.As(out _problem),
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
                                    new Glyph { Symbol = Symbols.Play, Style = Styles.Tool, Command = _runs }.As(out _run),
                                    new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Tool, Command = _copies }.As(out _duplicate),
                                    new Button { Style = Styles.Offer, Command = _offers }.As(out _button),
                                    new Switch { Style = Styles.Switch, IsSet = true, Command = _flips }.As(out _switch),
                                    new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Command = _bins }.As(out _delete)
                                }
                            }
                        }
                    }.As(out _inside)
                }
            };
        }

        /// <summary>The row itself, for the list to hold.</summary>
        public Element Root => _line;

        /// <summary>Shows <paramref name="entry" /> on this row.</summary>
        public void Update(StatusEntry entry)
        {
            _entry = entry;
            _inside.Padding = new Thickness(entry.Depth > 0 ? Inset : 0f, 0f, 0f, 0f);

            var pressable = entry.Command != 0;
            _open.CanExecute = pressable;
            _line.RichTip = entry.About;
            var wrong = entry.Problems > 0;

            _line.Style = wrong
                ? (entry.Chosen ? Styles.BrokenChosen : Styles.Broken)
                : entry.Running
                    ? (entry.Chosen ? Styles.RunningChosen : Styles.Running)
                    : entry.Parked
                        ? (entry.Chosen ? Styles.ParkedChosen : Styles.Parked)
                        : entry.Chosen
                            ? Styles.Chosen
                            : !pressable
                                ? Styles.Still
                                : entry.IsAction
                                    ? Styles.AddRow
                                    : Styles.Row;

            _summary.Text = entry.IsAction
                ? StartView.Plus + entry.Summary + (entry.Question.Length > 0 ? "…" : "")
                : entry.Summary;
            _summary.Color = entry.IsAction ? Palette.Muted : Palette.Ink;

            _light.Tint = wrong
                ? Palette.Problem
                : entry.IsAction
                    ? Palette.None
                    : entry.JustRan
                        ? Palette.Live
                        : Palette.Dim;

            _light.RichTip = entry.IsAction ? null : wrong ? _broken : entry.JustRan ? _ran : _idle;

            _status.Text = entry.Status;
            _lastRun.Text = entry.LastRun;

            var doing = entry.Running ? entry.StopCommand : entry.RunCommand;

            _run.Symbol = entry.Running ? Symbols.Stop : Symbols.Play;
            _run.Collapsed = doing == 0;
            _runs.CanExecute = doing != 0;
            _run.Tip = doing != 0 ? (entry.Running ? SequenceText.Stops() : SequenceText.Starts()) : null;

            _changes.CanExecute = entry.StatusCommand != 0;

            if (entry.StatusCommand != 0 && !ReferenceEquals(_opens, entry.Status))
            {
                _opens = entry.Status;
                _opensSays = "Change what this does";
            }

            _status.Tip = entry.StatusCommand != 0 ? _opensSays : null;

            var saying = !string.IsNullOrEmpty(entry.Problem);

            _detail.Collapsed = !saying
                && string.IsNullOrEmpty(entry.Status) && string.IsNullOrEmpty(entry.LastRun);

            _status.Collapsed = saying;

            _problem.Text = entry.Problem;
            _problem.Collapsed = !saying;
            _problem.Color = entry.ProblemIsWarning ? Palette.Notice : Palette.Problem;

            var marked = !string.IsNullOrEmpty(entry.Problem) || entry.Problems > 0
                || entry.Remarks > 0;

            var quietly = string.IsNullOrEmpty(entry.Problem)
                ? entry.Problems == 0
                : entry.ProblemIsWarning;

            _below.Collapsed = !marked;
            _below.Tint = quietly ? Palette.Notice : Palette.Problem;

            _switch.Collapsed = !entry.CanSwitch;
            _switch.IsOn = entry.SwitchIsOn;
            _switch.Style = entry.SwitchGovernsAll ? Styles.Governing : Styles.Switch;

            _flips.CanExecute = entry.CanSwitch;

            _switch.Tip = entry.CanSwitch
                ? Switching(entry.SwitchGovernsAll, entry.SwitchIsOn)
                : null;

            var copies = entry.DuplicateCommand != 0;
            _duplicate.Collapsed = !copies;
            _copies.CanExecute = copies;
            _duplicate.Tip = copies ? entry.DuplicateText : null;

            var offers = entry.ButtonCommand != 0;
            _button.Collapsed = !offers;
            _offers.CanExecute = offers;
            _button.RichTip = offers ? entry.ButtonAbout : null;

            if (offers)
                _button.Text = entry.ButtonText;

            var bins = entry.DeleteCommand != 0;
            _delete.Collapsed = !bins;
            _bins.CanExecute = bins;
            _delete.Tip = bins ? "Delete" : null;
        }

        void Open()
        {
            _view.Host.Send(_line, _entry.Command, _entry.Question);
        }

        void Run()
        {
            _view.Host.Send(_run, _entry.Running ? _entry.StopCommand : _entry.RunCommand, null);
        }

        void Change()
        {
            _view.Host.Send(_status, _entry.StatusCommand, null);
        }

        void Flip()
        {
            _view.Host.Switch(_switch, _entry.SwitchCommand, !_entry.SwitchIsOn);
        }

        void Copy()
        {
            _view.Host.Send(_duplicate, _entry.DuplicateCommand, null);
        }

        void Offer()
        {
            _view.Host.Send(_button, _entry.ButtonCommand, _entry.ButtonQuestion);
        }

        void Bin()
        {
            _view.Host.Send(_delete, _entry.DeleteCommand, null);
        }

        /// <summary>What pressing a switch would do, as a note on the switch.</summary>
        internal static string Switching(bool governsAll, bool isOn)
        {
            if (!governsAll)
                return isOn ? "Switch off" : "Switch on";

            return isOn
                ? "Switch the whole system off, stopping every sequence in it"
                : "Switch the whole system on";
        }
    }
}
