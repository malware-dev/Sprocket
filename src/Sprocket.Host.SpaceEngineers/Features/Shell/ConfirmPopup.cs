using System.Collections.Generic;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A short question hanging under whatever raised it, answered by pressing one of a few rows.</summary>
    public partial class ConfirmPopup : Popup
    {
        /// <summary>A question asking nothing yet.</summary>
        public ConfirmPopup()
        {
            CreateView();
        }

        /// <summary>Asks <paramref name="question" /> under <paramref name="anchor" />, offering <paramref name="answers" /> with <paramref name="taken" /> as the one Enter takes.</summary>
        /// <remarks>The question is markdown-lite, or a template filled from <paramref name="arguments" /> where they are given.</remarks>
        public void Ask(string question, Arguments arguments, IList<ChoiceEntry> answers, ChoiceEntry taken, Element anchor,
            WindowLayer layer)
        {
            var asks = !string.IsNullOrEmpty(question);
            _question.IsVisible = asks;
            _question.Document = !asks ? null : arguments == null ? Markdown.Parse(question) : Template.Parse(question);
            _question.Arguments = arguments;

            _answers.Children.Clear();
            for (var i = 0; i < answers.Count; i++)
            {
                var answer = answers[i];
                var enter = answer == taken;
                Label words;

                var row = new HStack
                {
                    Style = Styles.MenuRow,
                    Command = answer.Command,
                    Children = { new Label { Style = Styles.ListWord, Text = answer.Text }.As(out words) }
                };
                row.Set(LookStates.Chosen, enter);
                words.Set(LookStates.Chosen, enter);
                _answers.Children.Add(row);
            }

            Anchor = anchor;
            layer.Open(this);
        }
    }
}
