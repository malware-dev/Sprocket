using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    public sealed partial class CodexWindow
    {
        readonly List<Button> _entries = new List<Button>();

        Relay _stepsBack;
        Relay _stepsForward;
        Button _contents;
        Label _title;
        RichText _body;
        VStack _list;

        /// <summary>The bar it is moved by, holding back, forward, its contents, the term's name and the close cross, over the page that explains the term.</summary>
        void CreateView()
        {
            var texts = _editor.Texts;
            Style = Styles.CodexWindow;
            Placement = FixedSize.Size(Wide, High);

            _stepsBack = new Relay(StepBack);
            _stepsForward = new Relay(StepForward);

            var list = new VStack { Padding = new Thickness(12f, 8f) };
            var terms = _editor.Codex.All;
            var within = false;
            for (var i = 0; i < terms.Count; i++)
            {
                var term = terms[i];
                within |= term.Opens;
                var entry = new Button { Style = Entry(term, false), Text = term.Name, Command = new Relay(() => Go(term)) };
                _entries.Add(entry);

                // The terms of a section stand under its heading, in from it.
                var indent = within && !term.Opens ? Styles.CodexIndent : 0f;
                list.Children.Add(new HStack { Padding = new Thickness(indent, 0f, 0f, 0f), Children = { entry } });
            }

            Content = new VStack
            {
                Children =
                {
                    new TitleBar
                    {
                        Window = this,
                        Tip = new Tip(texts.CodexMove),
                        Content = new HStack
                        {
                            Background = Palette.Chrome,
                            Padding = new Thickness(12f, 8f),
                            Spacing = 8f,
                            Children =
                            {
                                new Glyph { Symbol = Symbols.Left, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Padding = EditorView.LevelWithCapitals, Command = _stepsBack, Tip = new Tip(texts.CodexBack) },
                                new Glyph { Symbol = Symbols.Right, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Padding = EditorView.LevelWithCapitals, Command = _stepsForward, Tip = new Tip(texts.CodexForward) },
                                new Button { Style = Styles.Button, Text = texts.CodexContents, Command = new Relay(() => List(!_listing)) }.As(out _contents),
                                new Label { Style = Styles.Name, Placement = Flex.Grow }.As(out _title),
                                new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = new Relay(Close), Tip = new Tip(texts.CodexClose) }
                            }
                        }
                    },
                    new Rule(1f, Palette.Line),
                    new ScrollView
                    {
                        Style = Styles.ScrollView,
                        Placement = Flex.Grow,
                        Content = new VStack
                        {
                            Children =
                            {
                                new RichText { Style = Styles.Codex }.As(out _body),
                                list.As(out _list)
                            }
                        }
                    }
                }
            };

            _editor.Codex.Wire(_body, Go);
        }
    }
}
