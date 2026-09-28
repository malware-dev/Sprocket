using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>What a sequence is doing: whether it just ran, its state or what is wrong with it, when it last ran, and what was skipped.</summary>
    /// <remarks>
    /// The tools that act on the whole sequence, run or stop, undo, redo, delete and its switch, are in the screen's header.
    /// Whoever holds this sets what it shows and calls <see cref="Show" />. The elements it is drawn with are declared in
    /// <c>SequenceBar.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class SequenceBar : HStack
    {
        /// <summary>A line saying nothing yet, filled in by setting what it shows and calling <see cref="Show" />.</summary>
        public SequenceBar()
        {
            Status = "";
            Problem = "";
            LastRun = "";
            Skipped = "";
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

        /// <summary>Shows what it has been told.</summary>
        /// <remarks>Runs on every refresh of the page, and sets only what it is told.</remarks>
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
        }
    }
}
