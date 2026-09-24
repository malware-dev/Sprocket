using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>One live thing on one line: what it is, what it is doing, and when it last did anything.</summary>
    /// <remarks>Core never nests entries more than one level deep, so one fixed indent serves every row.</remarks>
    public sealed class StatusRow
    {
        const float Inset = 35f;

        readonly HStack _line;
        readonly Dot _light;
        readonly Label _summary;
        readonly Label _status;
        readonly Glyph _run;
        readonly Label _lastRun;
        readonly Label _problem;
        readonly Switch _switch;
        readonly Glyph _duplicate;
        readonly Glyph _delete;
        readonly HStack _detail;
        readonly Glyph _below;
        string _opens;
        string _opensSays;

        /// <summary>An empty row, filled in by <see cref="Update" />.</summary>
        public StatusRow()
        {
            _line = new HStack
            {
                Spacing = Space.PadX,
                Background = Palette.Panel,
                Children =
                {
                    new Dot().As(out _light),
                    new VStack
                    {
                        Spacing = Space.Line * 0.6f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new HStack
                            {
                                Spacing = Space.Item,
                                Children =
                                {
                                    Widgets.Label("", Palette.Ink, Lettering.Body).As(out _summary),
                                    Widgets.Symbol(Symbols.Warning, Palette.Problem).As(out _below)
                                }
                            },
                            new HStack
                            {
                                Spacing = Space.Item,
                                Children =
                                {
                                    Widgets.Label("", Palette.Faint, Lettering.Tiny).As(out _status),
                                    Widgets.Label("", Palette.Problem, Lettering.Tiny).As(out _problem),
                                    Widgets.Label("", Palette.Faint, Lettering.Tiny).As(out _lastRun)
                                }
                            }.As(out _detail)
                        }
                    },
                    new HStack
                    {
                        Spacing = Space.Item,
                        Children =
                        {
                            Widgets.Centred(Symbols.Play, Palette.Faint).As(out _run),
                            Widgets.Centred(Symbols.Duplicate, Palette.Faint).As(out _duplicate),
                            new Switch(Lettering.Small).As(out _switch),
                            new Glyph(Symbols.Delete, Lettering.Symbol, Palette.Faint) { Highlight = Palette.ProblemWash }
                                .As(out _delete)
                        }
                    }
                }
            };
        }

        /// <summary>The row itself, for the list to hold.</summary>
        public Element Root => _line;

        /// <summary>Shows <paramref name="entry" /> on this row.</summary>
        public void Update(StatusEntry entry)
        {
            float side = Space.PadX + (entry.Depth > 0 ? Inset : 0f);
            _line.Padding = new Thickness(side, Space.PadY, side, Space.PadY);

            _line.Press = Deed.Sends(entry.Command);

            bool pressable = entry.Command != 0;

            var wrong = entry.Problems > 0;

            _line.Background = wrong
                ? Palette.ProblemWash
                : entry.Running
                    ? Palette.LiveWash
                    : entry.Parked
                        ? Palette.NoticeWash
                        : (pressable && !entry.IsAction ? Palette.Panel : Palette.None);

            if (!pressable)
                _line.Highlight = Palette.None;
            else if (entry.IsAction)
                _line.Highlight = Palette.Panel;
            else
                Widgets.Row(_line);

            _summary.Text = entry.IsAction ? StartView.Plus + entry.Summary : entry.Summary;
            _summary.Color = entry.IsAction ? Palette.Muted : Palette.Ink;
            _summary.FontSize = entry.IsAction ? Lettering.Small : Lettering.Body;

            _light.Background = wrong
                ? Palette.Problem
                : (entry.IsAction ? Palette.None : (entry.JustRan ? Palette.Live : Palette.Dim));

            _status.Text = entry.Status;
            _lastRun.Text = entry.LastRun;

            int doing = entry.Running ? entry.StopCommand : entry.RunCommand;

            _run.Symbol = entry.Running ? Symbols.Stop : Symbols.Play;
            _run.Collapsed = doing == 0;
            Widgets.Waiting(_run, doing, entry.Running ? SequenceText.Stops() : SequenceText.Starts());

            _status.Press = Deed.Sends(entry.StatusCommand);
            _status.Highlight = entry.StatusCommand != 0 ? Palette.Panel : Palette.None;

            if (entry.StatusCommand != 0 && !ReferenceEquals(_opens, entry.Status))
            {
                _opens = entry.Status;
                _opensSays = "Change what this does";
            }

            _status.Tip = entry.StatusCommand != 0 ? _opensSays : null;

            bool saying = !string.IsNullOrEmpty(entry.Problem);

            _detail.Collapsed = !saying
                && string.IsNullOrEmpty(entry.Status) && string.IsNullOrEmpty(entry.LastRun);

            _status.Collapsed = saying;

            _problem.Text = entry.Problem;
            _problem.Collapsed = !saying;
            _problem.Color = entry.ProblemIsWarning ? Palette.Notice : Palette.Problem;

            bool marked = !string.IsNullOrEmpty(entry.Problem) || entry.Problems > 0
                || entry.Remarks > 0;

            bool quietly = string.IsNullOrEmpty(entry.Problem)
                ? entry.Problems == 0
                : entry.ProblemIsWarning;

            _below.Collapsed = !marked;
            _below.Tint = quietly ? Palette.Notice : Palette.Problem;

            if (entry.CanSwitch)
                _switch.Show(entry.SwitchIsOn, entry.SwitchGovernsAll);
            else
                _switch.Hide();

            _switch.Press = entry.CanSwitch
                ? Deed.Switches(entry.SwitchCommand, !entry.SwitchIsOn)
                : Deed.Nothing;

            _switch.Tip = entry.CanSwitch
                ? Switching(entry.SwitchGovernsAll, entry.SwitchIsOn)
                : null;

            bool copies = entry.DuplicateCommand != 0;
            _duplicate.Collapsed = !copies;
            _duplicate.Press = Deed.Sends(entry.DuplicateCommand);
            _duplicate.Highlight = copies ? Palette.PanelHot : Palette.None;
            _duplicate.Tip = copies ? entry.DuplicateText : null;

            var bins = entry.DeleteCommand != 0;
            _delete.Collapsed = !bins;
            _delete.Press = Deed.Sends(entry.DeleteCommand);
            _delete.Tip = bins ? "Delete" : null;
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
