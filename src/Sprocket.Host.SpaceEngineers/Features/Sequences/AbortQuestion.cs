using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

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
        readonly Func<Element> _part;
        readonly Action<string, AbortLevel, bool> _answered;

        bool _done;

        /// <summary>
        /// A panel opening on what <paramref name="abort" /> says, hung from what <paramref name="part" /> finds, and
        /// <paramref name="answered" /> with the message and level it closes on and whether it was put away.
        /// </summary>
        public AbortQuestion(SprocketEditor editor, Abort abort, Func<Element> part, Action<string, AbortLevel, bool> answered)
        {
            _editor = editor;
            _part = part;
            _answered = answered;

            var texts = editor.Texts;
            var level = SentenceText.AbortLevelField(texts.AbortLevel);
            level.About = texts.Codex.SequenceAbortLevelAbout;

            _form = new FieldSet();
            _form.Show(new[] { SentenceText.AbortMessageField(texts.AbortMessage), level },
                new[] { abort.Message, SentenceText.AbortLevels[(int)abort.Level] }, null, texts);
            _form.Submitted += () => Finish(false);

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
                            Command = new RelayCommand(() => Finish(false))
                        }
                    }
                }
            };
            _window.Closed += _ => Finish(true);
        }

        /// <summary>The form the message and level are given in.</summary>
        public FieldSet Form => _form;

        /// <summary>Whether it is still waiting for its answer.</summary>
        public bool IsOpen => !_done;

        /// <summary>Hangs the panel from its part, once the part is on a screen; nothing where it hangs already or is answered.</summary>
        public void Hang()
        {
            if (_done || _window.IsOpen)
                return;

            var part = _part();
            if (part == null || part.Windows == null)
                return;

            _window.Anchor = part;
            _window.Open();
            _form.Take(_editor.Input);
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
