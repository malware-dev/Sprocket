using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>What an abort says and the level it shows at, given in a panel hung from the part of its sentence that says them.</summary>
    /// <remarks>
    /// Answered once, as it closes: Done or Enter answers with what the panel says, and Escape or a press anywhere else
    /// answers that it was put away.
    /// </remarks>
    public class AbortQuestion
    {
        readonly SprocketEditor _editor;
        readonly FieldSet _form;
        readonly Popup _window;
        readonly Action<string, AbortLevel, bool> _answered;

        bool _done;

        /// <summary>
        /// A panel opening on what <paramref name="abort" /> says, hung from what <paramref name="part" /> finds, and
        /// <paramref name="answered" /> with the message and level it closes on and whether it was put away.
        /// </summary>
        public AbortQuestion(SprocketEditor editor, Abort abort, Func<Element> part, Action<string, AbortLevel, bool> answered)
        {
            _editor = editor;
            _answered = answered;

            var texts = editor.Texts;
            var level = SentenceText.AbortLevelField(texts.AbortLevel);
            level.About = texts.Codex.SequenceAbortLevelAbout;

            _form = new FieldSet();
            _form.Show(new[] { SentenceText.AbortMessageField(texts.AbortMessage), level },
                new[] { abort.Message, SentenceText.AbortLevels[(int)abort.Level] }, null, texts);
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
            _window.FnAnchor = part;
            _window.Closed += reason => Finish(reason != CloseReason.Accepted);
        }

        /// <summary>The form the message and level are given in.</summary>
        public FieldSet Form => _form;

        /// <summary>Whether it is still waiting for its answer.</summary>
        public bool IsOpen => !_done;

        /// <summary>Hangs the panel from its part, once the part is on a screen, and follows the part made again; nothing once answered.</summary>
        /// <remarks>Called every tick: one look for the part.</remarks>
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

            _answered(_form.Fields[0].Text, SentenceText.AbortLevelOf(_form.Fields[1].Text), cancelled);
        }
    }
}
