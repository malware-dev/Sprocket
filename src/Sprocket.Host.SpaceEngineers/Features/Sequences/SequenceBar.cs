using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The line saying what a sequence is doing, when it last ran, and what was skipped.</summary>
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

        /// <summary>What is wrong with it, or worth a word, said in place of its status, or empty.</summary>
        public string Problem { get; set; }

        /// <summary>How much <see cref="Problem" /> matters: a problem, a warning or a note.</summary>
        public NoticeLevel Level { get; set; } = NoticeLevel.Problem;

        /// <summary>When it last ran, or empty.</summary>
        public string LastRun { get; set; }

        /// <summary>Whether it ran a moment ago.</summary>
        public bool JustRan { get; set; }

        /// <summary>What was asked of it and did not happen, or empty.</summary>
        public string Skipped { get; set; }

        /// <summary>Shows what it has been told.</summary>
        /// <remarks>Runs on every refresh of the page.</remarks>
        public void Show(Texts texts)
        {
            var wrong = Problem.Length != 0;
            _status.IsVisible = !wrong;
            _status.Text = Status;
            _problem.IsVisible = wrong;
            _problem.Text = Problem;
            _problem.Color = RemarkColors.Of(Level, Palette.Muted);
            _lastRun.Text = LastRun;
            _light.Tint = JustRan ? Palette.Live : Palette.Dim;

            _skipped.IsVisible = Skipped.Length != 0;
            _skipped.Text = Skipped;
        }
    }
}
