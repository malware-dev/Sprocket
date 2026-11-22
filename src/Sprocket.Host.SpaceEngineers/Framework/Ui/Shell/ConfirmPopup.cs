using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Menus;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>A short question hanging under whatever raised it, answered by pressing one of a few rows.</summary>
    public partial class ConfirmPopup : Menu
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

            while (Items.Count > 1)
                Items.Remove(Items[Items.Count - 1]);

            Fill(this, answers, taken);
            Anchor = anchor;
            layer.Open(this);
        }

        // Puts answers into menu, an answer with answers within it opening them as a menu of its own.
        static void Fill(Menu menu, IList<ChoiceEntry> answers, ChoiceEntry taken)
        {
            foreach (var answer in answers)
            {
                if (answer.IsRule)
                {
                    menu.Items.Add(new Separator());
                    continue;
                }

                if (answer.Within.Count == 0)
                {
                    menu.Items.Add(new MenuItem { Style = Styles.MenuItem, Text = answer.Text, Command = answer.Command, IsChosen = answer == taken });
                    continue;
                }

                var within = new Menu { Style = Styles.Menu };
                Fill(within, answer.Within, taken);
                menu.Items.Add(new MenuItem { Style = Styles.MenuItem, Text = answer.Text, Submenu = within, IsChosen = taken != null && answer.Holds(taken) });
            }
        }
    }
}
