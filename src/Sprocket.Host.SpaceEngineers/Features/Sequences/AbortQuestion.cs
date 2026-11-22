using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
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
        readonly Action<string, string, Expression, AbortLevel, bool> _answered;
        ValuesQuestion _making;

        bool _done;

        /// <summary>
        /// A panel opening on what <paramref name="abort" />, a step of <paramref name="system" />, says, hung from what
        /// <paramref name="part" /> finds, and <paramref name="answered" /> with the message, the setting saying it, the level it
        /// closes on and whether it was put away.
        /// </summary>
        /// <remarks>A text setting may say the message where <paramref name="system" /> is a blueprint.</remarks>
        public AbortQuestion(SprocketEditor editor, AutomationSystem system, Abort abort, Func<Element> part,
            Action<string, string, Expression, AbortLevel, bool> answered)
        {
            _editor = editor;
            _answered = answered;

            var texts = editor.Texts;
            var level = SentenceText.AbortLevelField(texts.AbortLevel);
            level.About = texts.Codex.SequenceAbortLevelAbout;
            var message = SentenceText.AbortMessageField(texts.AbortMessage);

            _form = new FieldSet();
            if (system != null && system.IsBlueprint)
                _form.Sourced = field => field.Name == message.Name;

            _form.Show(new[] { message, level }, new[] { abort.Message, SentenceText.AbortLevels[(int)abort.Level] }, null, texts);
            if (abort.Setting.Length > 0)
                _form.Choose(0, abort.Setting, SettingSources.Default(system, abort.Setting));
            if (abort.Expression != null)
                _form.Hold(0, Literal.Worked(abort.Expression), system == null ? abort.Expression.Written : ExpressionShown.Of(editor, system, abort.Expression));

            _form.Sourcing += (at, mark) => SettingSources.Offer(editor, system, type => type == "text", _form.SettingAt(at),
                (setting, said) => _form.Choose(at, setting, said),
                () => _making = SettingSources.New(editor, system, "text", Literal.Of(_form.ValueAt(at)), () => mark,
                    made => _form.Choose(at, made, SettingSources.Default(system, made)), () => _making = null),
                () => SettingSources.Build(editor, system, _form, at, "text"));
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

            _making?.Hang();
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
            _making?.Finish(true);
            _window.Close();

            var held = _form.HeldAt(0);
            _answered(_form.Fields[0].Text, _form.SettingAt(0), held.IsExpression ? held.Tree : null, SentenceText.AbortLevelOf(_form.Fields[1].Text), cancelled);
        }
    }
}
