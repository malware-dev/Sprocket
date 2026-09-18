using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Lists
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

        /// <summary>Another of what this row is. Absent on rows where that means nothing.</summary>
        readonly Glyph _duplicate = Widgets.Symbol(Symbols.Duplicate, Palette.Faint);

        /// <summary>The controls at the right edge, laid out leftwards from the switch.</summary>
        readonly Stack _tools = Widgets.RowOf(Space.Item);
        readonly Stack _text = Widgets.Column(Space.Line * 0.6f);
        readonly Stack _detail = Widgets.RowOf(Space.Item);

        /// <summary>
        /// Says that something below this row is wrong, without saying what.
        /// </summary>
        /// <remarks>
        /// Immediately after the name, rather than under it or at the far edge. A row that owns a
        /// problem puts the sentence on its own line; a row that merely contains one has nothing to
        /// say yet, and the point is to tell somebody which way to go rather than to explain here.
        /// </remarks>
        readonly Glyph _below = Widgets.Symbol(Symbols.Warning, Palette.Problem);

        readonly Stack _heading = Widgets.RowOf(Space.Item);

        public StatusRow()
        {
            _detail.Children.Add(_status);
            _detail.Children.Add(_lastRun);

            // Hugging the name, not filling the row. A mark at the far right lands beside the
            // switch and reads as belonging to it, which is a different claim entirely.
            _heading.Children.Add(_summary);
            _heading.Children.Add(_below);

            _text.Children.Add(_heading);
            _text.Children.Add(_detail);
            _text.Children.Add(_problem);

            _line.Spacing = Space.PadX;
            _line.Background = Palette.Panel;
            _line.Left = _light;
            _line.Fill = _text;
            // The switch stays last, so it is the one control whose place nothing can move: this row
            // lays out leftwards from its right edge, and the switch is what somebody is pressing when
            // the copy button beside it appears or goes.
            _tools.Children.Add(_duplicate);
            _tools.Children.Add(_switch);
            _line.Right = _tools;
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
            _line.Press = Deed.Sends(entry.Command);

            bool pressable = entry.Command != 0;

            // A row leading to something wrong is washed rather than merely marked. A small glyph
            // beside a name is something you find when you already suspect; the point of this is to
            // be seen by somebody who does not yet know to look.
            var wrong = entry.Problems > 0;

            // And a row that is going right now is washed the same way, in the other colour, for the
            // same reason: it is the row somebody scanning a list is looking for. The same green the
            // step being run wears one screen in, so the two agree about what live looks like.
            //
            // Broken outranks running, which the two can only be at once for the instant between a
            // block going away and the run abandoning the step that named it.
            //
            // A row that adds something is the same button as the one under a sentence, because it is
            // the same act: no fill, quiet words, and a highlight that says it answers to a press.
            _line.Background = wrong
                ? Palette.ProblemWash
                : entry.Running
                    ? Palette.LiveWash
                    : (pressable && !entry.IsAction ? Palette.Panel : Palette.None);

            if (!pressable)
                _line.Highlight = Palette.None;
            else if (entry.IsAction)
                // The quiet step, because this row has no fill to brighten. The same step the button
                // under a sentence takes, since it is the same act.
                _line.Highlight = Palette.Panel;
            else
                Widgets.Row(_line);

            // The same plus the start block uses, for the same reason: this row adds something. Core marks
            // it as an action and leaves the drawing to us.
            _summary.Text = entry.IsAction ? StartView.Plus + entry.Summary : entry.Summary;
            _summary.Color = entry.IsAction ? Palette.Muted : Palette.Ink;
            _summary.Scale = entry.IsAction ? Lettering.Small : Lettering.Body;

            // A row that does something has no state to show, so it gets no light - only the space where
            // one would be, which keeps its text lined up with its neighbours.
            // Broken outranks recently-run. A green light on a row that cannot work is the screen
            // contradicting itself, and the light is the first thing the eye lands on.
            _light.Background = wrong
                ? Palette.Problem
                : (entry.IsAction ? Palette.None : (entry.JustRan ? Palette.Live : Palette.Dim));

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

            // A warning reads as a remark, not as an alarm. Same line, same place, quieter colour - the
            // difference between "nothing starts this yet" and "this affects a block that is gone".
            _problem.Color = entry.ProblemIsWarning ? Palette.Notice : Palette.Problem;

            // Nothing wrong is the ordinary case, so it takes no room rather than drawing blank - a
            // column of rows each reserving space for a mark reads as a column of marks. Keyed on there
            // being words rather than on the count, because a warning has words and is not counted.
            _below.Collapsed = string.IsNullOrEmpty(entry.Problem);
            _below.Tint = entry.ProblemIsWarning ? Palette.Notice : Palette.Problem;

            // Says what state it is in, the way the game's own on/off control does. The light on the
            // left of this row says whether the thing is doing something right now; this says whether it
            // has been left switched on, which is not the same question and is the one a switch answers.
            if (entry.CanSwitch)
                _switch.Show(entry.SwitchIsOn, entry.SwitchGovernsAll);
            else
                _switch.Hide();

            _switch.Press = entry.CanSwitch
                ? Deed.Switches(entry.SwitchCommand, !entry.SwitchIsOn)
                : Deed.Nothing;

            // Says the act and how far it reaches, and never which row it is on - the pointer is on
            // that row. Scope is what a column of identical switches does not show: SwitchGovernsAll is
            // core's answer to which of them stops a whole system, and reading the indent instead would
            // be guessing from a layout this target chose itself.
            //
            // Only the off direction carries the consequence. Switching a system on does not start
            // anything by itself, where switching it off stops everything in it at once, and a note that
            // warned about both equally would be a note nobody finished reading.
            _switch.Tip = entry.CanSwitch
                ? Switching(entry.SwitchGovernsAll, entry.SwitchIsOn)
                : null;

            // Out of the layout where it is not offered, rather than dimmed. A system's heading row and
            // a row that adds something are not things there can be another of, and a column of greyed
            // controls beside the ones that work reads as a screen half broken.
            bool copies = entry.DuplicateCommand != 0;
            _duplicate.Collapsed = !copies;
            _duplicate.Press = Deed.Sends(entry.DuplicateCommand);
            _duplicate.Highlight = copies ? Palette.PanelHot : Palette.None;
            _duplicate.Tip = copies ? entry.DuplicateText : null;
        }

        /// <summary>
        /// What pressing a switch would do.
        /// </summary>
        /// <remarks>
        /// Two bools and nothing else, so these are constants rather than strings joined per row per
        /// frame. That is a consequence of the note no longer naming what it is on: the name was the
        /// only part that varied.
        /// </remarks>
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
