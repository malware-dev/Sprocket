using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>A length being given in a panel, hung from the part of a sentence it is the length of whenever that part is on a screen.</summary>
    /// <remarks>
    /// Answered once, as it closes: Done or Enter answers with what the panel says, and Escape or a press anywhere else
    /// answers that it was put away.
    /// </remarks>
    public class LengthQuestion
    {
        readonly SprocketEditor _editor;
        readonly DurationPanel _panel;
        readonly Popup _window;
        readonly Action<Duration?, string, bool> _answered;
        ValuesQuestion _making;

        bool _done;

        /// <summary>
        /// A panel opening on <paramref name="start" />, or on the setting <paramref name="setting" /> of <paramref name="system" /> where
        /// it is not empty, cautioning about cost where it is a <paramref name="rate" />, hung from what <paramref name="chip" /> finds,
        /// and <paramref name="answered" /> with the length and the setting it closes on and whether it was put away; where <paramref name="expression" /> is given, the link mark offers an expression, which it is carried out for.
        /// </summary>
        /// <remarks>A setting may give the length where <paramref name="system" /> is a blueprint.</remarks>
        public LengthQuestion(SprocketEditor editor, AutomationSystem system, Duration start, string setting, bool rate, Func<Element> chip,
            Action<Duration?, string, bool> answered, Action expression = null)
        {
            _editor = editor;
            _answered = answered;

            // A length that a setting gives is opened on the setting's default, which a fixed length chosen instead starts from.
            var held = string.IsNullOrEmpty(setting) ? null : system?.FindPart(setting);
            if (held != null && held.HasDefault && held.Default.Kind == ValueKind.Duration)
                start = held.Default.Length;

            _panel = new DurationPanel(editor.Texts, editor.Input, start) { Cautions = rate, Sourced = system != null && system.IsBlueprint };
            if (!string.IsNullOrEmpty(setting) && _panel.Sourced)
                _panel.Choose(setting, SettingSources.Default(system, setting));

            _panel.Sourcing += mark => SettingSources.Offer(editor, system, type => type == "duration", _panel.Setting, _panel.Choose,
                () => _making = SettingSources.New(editor, system, "duration", Literal.Of(_panel.Length ?? Duration.OfSeconds(1)), () => mark,
                    made => _panel.Choose(made, SettingSources.Default(system, made)), () => _making = null), expression);
            _panel.Submitted += Accept;
            _panel.Cancelled += () => Finish(true);
            _panel.Switched += editor.Prefer;

            _window = new Popup
            {
                Style = Styles.Panel,
                // Done laid over the panel's bottom right corner, on the line saying the length in the other unit.
                Content = new ZStack
                {
                    Children =
                    {
                        _panel,
                        new Button
                        {
                            Style = Styles.Primary,
                            Text = editor.Texts.ChromeDone,
                            Align = Align.Right | Align.Bottom,
                            Command = new RelayCommand(Accept)
                        }
                    }
                }
            };
            _window.FnAnchor = chip;
            _window.Closed += reason => Finish(reason != CloseReason.Accepted);
        }

        /// <summary>The panel the length is given in.</summary>
        public DurationPanel Panel => _panel;

        /// <summary>Whether it is still waiting for its answer.</summary>
        public bool IsOpen => !_done;

        /// <summary>Hangs the panel from its chip, once the chip is on a screen, and follows the chip made again; nothing once answered.</summary>
        /// <remarks>Called every tick: one look for the chip.</remarks>
        public void Hang()
        {
            if (_done)
                return;

            _making?.Hang();
            var opening = !_window.IsOpen;
            _window.Hang();
            if (!opening || !_window.IsOpen)
                return;

            if (_editor.Input != null)
                _editor.Input.Focus(_panel.Box);

            _panel.Box.SelectAll();
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
            _answered(_panel.Length, _panel.Setting, cancelled);
        }
    }
}
