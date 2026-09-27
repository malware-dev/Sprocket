using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>What a sequence is doing, and the tools that act on the whole of it: run or stop, undo, redo, delete, and its switch.</summary>
    /// <remarks>
    /// Nothing moves when the sequence is switched on or off, starts or stops: a control with nothing to do goes quiet
    /// where it stands, because a bar that rearranges itself under somebody is one they have to read again. Whoever
    /// holds it sets what it shows and calls <see cref="Show" />. The elements it is drawn with are declared in
    /// <c>SequenceBar.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class SequenceBar : HStack
    {
        string _undoSaid;
        string _undoFrom;
        string _undoTip;
        string _redoSaid;
        string _redoFrom;
        string _redoTip;

        /// <summary>A bar saying nothing yet, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public SequenceBar()
        {
            Status = "";
            Problem = "";
            LastRun = "";
            Skipped = "";
            UndoText = "";
            RedoText = "";
            CreateView();
        }

        /// <summary>What it is doing, in a word or two.</summary>
        public string Status { get; set; }

        /// <summary>What is wrong with it, said in place of its status, or empty.</summary>
        public string Problem { get; set; }

        /// <summary>Whether <see cref="Problem" /> is a warning rather than something broken.</summary>
        public bool ProblemIsWarning { get; set; }

        /// <summary>When it last ran, or empty.</summary>
        public string LastRun { get; set; }

        /// <summary>Whether it ran a moment ago.</summary>
        public bool JustRan { get; set; }

        /// <summary>What was asked of it and did not happen, or empty.</summary>
        public string Skipped { get; set; }

        /// <summary>Whether it is running now, which turns the run mark into a stop.</summary>
        public bool Running { get; set; }

        /// <summary>What running it by hand does, or null where it cannot be run from here.</summary>
        public ICommand Runs { get; set; }

        /// <summary>What stopping it does, or null where it cannot be stopped from here.</summary>
        public ICommand Stops { get; set; }

        /// <summary>What undoing the last change does, or null while there is nothing to walk back.</summary>
        public ICommand Undoes { get; set; }

        /// <summary>The change undoing would take back.</summary>
        public string UndoText { get; set; }

        /// <summary>What redoing does, or null while there is nothing to put back.</summary>
        public ICommand Redoes { get; set; }

        /// <summary>The change redoing would put back.</summary>
        public string RedoText { get; set; }

        /// <summary>What deleting the whole sequence does, or null where it cannot be deleted.</summary>
        public ICommand Deletes { get; set; }

        /// <summary>Whether it is switched on.</summary>
        public bool SwitchIsOn { get; set; }

        /// <summary>What its switch does, or null where it cannot be switched from here.</summary>
        public ICommand Flips { get; set; }

        /// <summary>The run mark, the one control here that turns into another.</summary>
        public Element RunPart => _run;

        /// <summary>Shows what it has been told, saying its fixed words in <paramref name="texts" />.</summary>
        /// <remarks>Runs on every refresh of the page, so a note is joined only when what it names has changed.</remarks>
        public void Show(Texts texts)
        {
            // One of the two, never both and never neither, so the line is the same height whatever is happening.
            var wrong = Problem.Length != 0;
            _status.IsVisible = !wrong;
            _status.Text = Status;
            _problem.IsVisible = wrong;
            _problem.Text = Problem;
            _problem.Color = ProblemIsWarning ? Palette.Notice : Palette.Problem;
            _lastRun.Text = LastRun;
            _light.Tint = JustRan ? Palette.Live : Palette.Dim;

            _skipped.IsVisible = Skipped.Length != 0;
            _skipped.Text = Skipped;

            // One control, two acts, and never both at once: a sequence that is going is one to stop and one that is
            // not is one to start.
            var doing = Running ? Stops : Runs;
            _run.Symbol = Running ? Symbols.Stop : Symbols.Play;
            _run.IsVisible = doing != null;
            _run.Command = doing;
            _run.Tip = Tip.For(_run.Tip, doing == null ? null : Running ? SequenceText.Stops() : SequenceText.Starts());

            if (!ReferenceEquals(_undoSaid, UndoText) || !ReferenceEquals(_undoFrom, texts.SequenceUndo))
            {
                _undoSaid = UndoText;
                _undoFrom = texts.SequenceUndo;
                _undoTip = Template.Fill(_undoFrom, "what", UndoText);
            }

            if (!ReferenceEquals(_redoSaid, RedoText) || !ReferenceEquals(_redoFrom, texts.SequenceRedo))
            {
                _redoSaid = RedoText;
                _redoFrom = texts.SequenceRedo;
                _redoTip = Template.Fill(_redoFrom, "what", RedoText);
            }

            Quiet(_undo, Undoes, Undoes == null ? null : _undoTip);
            Quiet(_redo, Redoes, Redoes == null ? null : _redoTip);
            Quiet(_delete, Deletes, Deletes == null ? null : texts.SequenceDelete);

            // The switch says the state and its note says the act, so it cannot be read the wrong way round.
            _switch.IsOn = SwitchIsOn;
            _switch.Says(texts.SwitchOn, texts.SwitchOff);
            _switch.Command = Flips;
            _switch.IsEnabled = Flips != null;
            _switch.Tip = Tip.For(_switch.Tip, Flips == null ? null : StatusRow.Switching(texts, false, SwitchIsOn));
        }

        static void Quiet(Glyph mark, ICommand command, string says)
        {
            mark.Command = command;
            mark.IsEnabled = command != null;
            mark.Tip = Tip.For(mark.Tip, says);
        }
    }
}
