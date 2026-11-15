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
        readonly Action<Duration?, bool> _answered;

        bool _done;

        /// <summary>
        /// A panel opening on <paramref name="start" />, cautioning about cost where it is a <paramref name="rate" />, hung from
        /// what <paramref name="chip" /> finds, and <paramref name="answered" /> with the length it closes on and whether it was
        /// put away.
        /// </summary>
        public LengthQuestion(SprocketEditor editor, Duration start, bool rate, Func<Element> chip, Action<Duration?, bool> answered)
        {
            _editor = editor;
            _answered = answered;

            _panel = new DurationPanel(editor.Texts, editor.Input, start) { Cautions = rate };
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
            _window.Close();
            _answered(_panel.Length, cancelled);
        }
    }
}
