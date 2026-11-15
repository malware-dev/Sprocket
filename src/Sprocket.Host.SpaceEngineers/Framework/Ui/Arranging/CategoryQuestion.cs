using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>A category's name and whether what is under it runs one at a time, given in a panel hung from its name.</summary>
    /// <remarks>
    /// Answered once, as it closes: Done or Enter answers with what the panel says, and Escape or a press anywhere else
    /// answers that it was put away.
    /// </remarks>
    public sealed class CategoryQuestion
    {
        readonly SprocketEditor _editor;
        readonly FieldSet _form;
        readonly Popup _window;
        readonly Action<string, bool, bool> _answered;

        bool _done;

        /// <summary>
        /// A panel opening on the category <paramref name="name" />, running one at a time where <paramref name="oneAtATime" />,
        /// hung from what <paramref name="plate" /> finds, and <paramref name="answered" /> with the name and setting it closes on
        /// and whether it was put away.
        /// </summary>
        public CategoryQuestion(SprocketEditor editor, string name, bool oneAtATime, Func<Element> plate, Action<string, bool, bool> answered)
        {
            _editor = editor;
            _answered = answered;

            var texts = editor.Texts;
            var called = Field.Called(texts.CategoryName);
            called.Needed = true;
            var onOff = new[] { Literal.Of(true).Said, Literal.Of(false).Said };
            var apart = Field.Among(texts.CategoryOneAtATime, onOff, onOff);
            apart.About = texts.Codex.CategoryOneAtATimeAbout;

            _form = new FieldSet();
            _form.Show(new[] { called, apart }, new[] { name, Literal.Of(oneAtATime).Said }, null, texts);
            _form.Submitted += Accept;

            _window = new Popup
            {
                Style = Styles.Panel,
                Content = new VStack
                {
                    Placement = FixedSize.Width(Sizes.Popup),
                    Base = { Spacing = Gaps.Items },
                    Children =
                    {
                        _form,
                        new Button
                        {
                            Style = Styles.Primary,
                            Text = texts.ChromeDone,
                            Align = Align.Right,
                            Command = new RelayCommand(Accept)
                        }
                    }
                }
            };
            _window.FnAnchor = plate;
            _window.Closed += reason => Finish(reason != CloseReason.Accepted);
        }

        /// <summary>The form the name and setting are given in.</summary>
        public FieldSet Form => _form;

        /// <summary>Whether it is still waiting for its answer.</summary>
        public bool IsOpen => !_done;

        /// <summary>Hangs the panel from the name, once the name is on a screen, and follows the name made again; nothing once answered.</summary>
        /// <remarks>Called every tick: one look for the name.</remarks>
        public void Hang()
        {
            if (_done)
                return;

            var opening = !_window.IsOpen;
            _window.Hang();
            if (opening && _window.IsOpen)
                _form.Take(_editor.Input);
        }

        /// <summary>Answers with what the panel says, closing it as finished with.</summary>
        void Accept()
        {
            if (_window.IsOpen)
                _window.Accept();
            else
                Finish(false);
        }

        /// <summary>Puts the panel away and answers with what it says, or that it was put away where <paramref name="cancelled" />.</summary>
        /// <remarks>Answers once, however often it is called.</remarks>
        public void Finish(bool cancelled)
        {
            if (_done)
                return;

            _done = true;
            _window.Close();

            _answered(_form.Fields[0].Text, _form.Fields[1].Text == Literal.Of(true).Said, cancelled);
        }
    }
}
