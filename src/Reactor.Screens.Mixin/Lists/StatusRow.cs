using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Lists
{
    /// <summary>
    /// One live thing, on one line: what it is, what it is doing, when it last did anything.
    /// </summary>
    /// <remarks>
    /// Indented by <c>Depth</c>, which core promises is never more than one level, so a fixed indent is
    /// right rather than lazy - there is no tree here to reinvent.
    /// </remarks>
    public sealed class StatusRow
    {
        /// <summary>
        /// How far a nested row steps in, on both sides.
        /// </summary>
        /// <remarks>
        /// Both, not just the left. The left edge already says "this belongs to the row above"; the right
        /// edge did not, so a switch on a nested row landed in the same column as the switch governing it,
        /// and nothing but reading the labels told them apart.
        /// </remarks>
        const float Inset = 35f;

        readonly Bar _line = new Bar();
        readonly Dot _light = new Dot();
        readonly Label _summary = Widgets.Label("", Palette.Ink, Lettering.Body);
        readonly Label _status = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly Label _lastRun = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly Label _problem = Widgets.Label("", Palette.Problem, Lettering.Tiny);
        readonly Switch _switch = new Switch(Lettering.Small);
        readonly Stack _text = Widgets.Column(Space.Line * 0.6f);
        readonly Stack _detail = Widgets.RowOf(Space.Item);

        public StatusRow()
        {
            _detail.Children.Add(_status);
            _detail.Children.Add(_lastRun);

            _text.Children.Add(_summary);
            _text.Children.Add(_detail);
            _text.Children.Add(_problem);

            _line.Spacing = Space.PadX;
            _line.Background = Palette.Panel;
            _line.Left = _light;
            _line.Fill = _text;
            _line.Right = _switch;
        }

        public Element Root
        {
            get { return _line; }
        }

        public void Update(StatusEntry entry)
        {
            float side = Space.PadX + (entry.Depth > 0 ? Inset : 0f);
            _line.Padding = new Thickness(side, Space.PadY, side, Space.PadY);

            // A system's row carries no command - it is a heading over the sequences under it, and a
            // heading that answered to a press would be a promise it cannot keep. So it is not drawn as
            // a panel either: a panel says "press me", and the only way to find out it was inert would be
            // to press it. The light and the switch stay, because those are real.
            bool pressable = entry.Command != 0;
            _line.Background = pressable ? Palette.Panel : Palette.None;
            _line.Highlight = pressable ? Palette.PanelHot : Palette.None;

            // The same plus the start block uses, for the same reason: this row adds something. Core marks
            // it as an action and leaves the drawing to us.
            _summary.Text = entry.IsAction ? StartView.Plus + entry.Summary : entry.Summary;
            _summary.Color = entry.IsAction ? Palette.Muted : Palette.Ink;

            // A row that does something has no state to show, so it gets no light - only the space where
            // one would be, which keeps its text lined up with its neighbours.
            _light.Background = entry.IsAction
                ? Palette.None
                : (entry.JustRan ? Palette.Live : Palette.Dim);

            _status.Text = entry.Status;
            _lastRun.Text = entry.LastRun;

            // Out of the layout when there is nothing to say, not merely blank. A row that always
            // reserved both of these would be three lines tall to show one, and a list of them would be
            // a column of mostly empty panel.
            _detail.Collapsed = string.IsNullOrEmpty(entry.Status) && string.IsNullOrEmpty(entry.LastRun);

            // Its own line rather than a replacement for the status. What went wrong and what it is doing
            // are two different readings, and losing one to show the other is how a diagnosis screen lies.
            _problem.Text = entry.Problem;
            _problem.Collapsed = string.IsNullOrEmpty(entry.Problem);

            // Says what state it is in, the way the game's own on/off control does. The light on the
            // left of this row says whether the thing is doing something right now; this says whether it
            // has been left switched on, which is not the same question and is the one a switch answers.
            if (entry.CanSwitch)
                _switch.Show(entry.SwitchIsOn, entry.SwitchGovernsAll);
            else
                _switch.Hide();
        }

        /// <summary>What pressing a switch would do, said in full.</summary>
        internal static string Switching(string name, bool governsAll, bool isOn)
        {
            if (!governsAll)
                return isOn ? "Switch " + name + " off" : "Switch " + name + " on";

            return isOn
                ? "Switch the whole " + name + " system off, stopping every sequence in it"
                : "Switch the whole " + name + " system on";
        }

        public void Collect(List<Hit> hits, StatusEntry entry)
        {
            // No command means there is nothing to press. A system's own row is a heading over the ones of
            // it, and a heading that answers to a press is a promise it cannot keep.
            if (entry.Command != 0)
            {
                var line = new Hit(_line.Bounds, entry.Command);
                line.Hot = _line;
                hits.Add(line);
            }

            // Added after the row, so the switch wins where the two overlap.
            if (entry.CanSwitch)
            {
                Hit toggle = Hit.Switch(_switch.Bounds, entry.SwitchCommand, !entry.SwitchIsOn);
                toggle.Hot = _switch;

                // Named, and told apart, because a column of these all say ON and OFF and nothing else
                // separates the one governing a whole system from the one governing a single sequence
                // under it. SwitchGovernsAll is core's answer to exactly that question - reading the
                // indent instead would be guessing from a layout this target chose itself.
                //
                // Only the off direction carries the consequence. Switching a system on does not start
                // anything by itself, where switching it off stops everything in it at once, and a note
                // that warned about both equally would be a note nobody finished reading.
                toggle.Tip = Switching(entry.Summary, entry.SwitchGovernsAll, entry.SwitchIsOn);

                hits.Add(toggle);
            }
        }
    }
}
