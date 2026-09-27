using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A short question hanging under whatever raised it, answered by pressing one of a few rows.</summary>
    /// <remarks>
    /// It reads as the control that asked opening, rather than as the screen stopping: a question in the middle of the
    /// screen takes the eye away from the row it is about, and then has to say which row that was. A press anywhere else
    /// puts it away without an answer, and so does Escape. The elements it is drawn with are declared in
    /// <c>Confirm.View.cs</c>.
    /// </remarks>
    public partial class Confirm : Popup
    {
        /// <summary>A question asking nothing yet.</summary>
        public Confirm()
        {
            CreateView();
        }

        /// <summary>
        /// Asks <paramref name="question" /> under <paramref name="anchor" />, over the screen <paramref name="layer" />
        /// belongs to, offering <paramref name="answers" /> in order and marking <paramref name="taken" /> as the one Enter
        /// takes.
        /// </summary>
        /// <remarks>The rows are made afresh for each question, which appears when somebody presses something and lives for one answer.</remarks>
        public void Ask(string question, IList<ChoiceEntry> answers, ChoiceEntry taken, Element anchor, WindowLayer layer)
        {
            _question.Text = question;
            _question.IsVisible = !string.IsNullOrEmpty(question);

            _answers.Children.Clear();
            for (var i = 0; i < answers.Count; i++)
            {
                var answer = answers[i];
                var enter = answer == taken;

                _answers.Children.Add(new HStack
                {
                    Style = enter ? Styles.MenuRowTaken : Styles.MenuRow,
                    Command = answer.Command,
                    Children = { new Label { Style = enter ? Styles.ListWordTaken : Styles.ListWord, Text = answer.Text } }
                });
            }

            Anchor = anchor;
            layer.Open(this);
        }
    }
}
