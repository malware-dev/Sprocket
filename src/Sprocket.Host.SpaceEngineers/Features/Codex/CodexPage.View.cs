using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    public sealed partial class CodexPage
    {
        const float Margin = 280f;

        readonly List<Button> _listed = new List<Button>();

        Label _lead;
        RichText _body;

        /// <summary>Every term down the left margin, the one being read marked, beside its sentence set apart over the page that explains it.</summary>
        protected override Element CreateView()
        {
            var margin = new VStack { Placement = Fixed.Wide(Margin), Align = Align.Top };
            var terms = Editor.Codex.All;
            for (var i = 0; i < terms.Count; i++)
            {
                var term = terms[i];
                var button = new Button { Style = Styles.CodexEntry, Text = term.Name, Command = new Relay(delegate { Go(term); }) };
                _listed.Add(button);
                margin.Children.Add(new HStack { Children = { button } });
            }

            return new HStack
            {
                Spacing = 16f,
                Children =
                {
                    margin,
                    new VStack
                    {
                        Placement = Flex.Grow,
                        Align = Align.Top,
                        Spacing = 8f,
                        Children =
                        {
                            new Label { Style = Styles.Lead, Wraps = true }.As(out _lead),
                            new RichText { Style = Styles.Description }.As(out _body)
                        }
                    }
                }
            };
        }
    }
}
