using System;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>A length being given in a panel, hung from the part of a sentence it is the length of whenever that part is on a screen.</summary>
    /// <remarks>
    /// The page's whether or not it hangs anywhere, so the editor gives a length the same way without a screen. It is answered
    /// once, as it closes: Done or Enter answers with what the panel says, and Escape or a press anywhere else answers that
    /// it was put away, as every popup is. Done is drawn as the button Enter presses.
    /// </remarks>
    public class LengthQuestion
    {
        readonly SprocketEditor _editor;
        readonly DurationPanel _panel;
        readonly Popup _window;
        readonly Func<Element> _chip;
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
            _chip = chip;
            _answered = answered;

            _panel = new DurationPanel(editor.Texts, editor.Input, start) { Cautions = rate };
            _panel.Submitted += delegate { Finish(false); };
            _panel.Cancelled += delegate { Finish(true); };
            _panel.Switched += editor.Prefer;

            _window = new Popup
            {
                Style = Styles.Panel,
                // Done laid over the panel's bottom right corner, on the line saying the length in the other unit.
                Content = new Layers
                {
                    Children =
                    {
                        _panel,
                        new Button
                        {
                            Style = Styles.Primary,
                            Text = editor.Texts.ChromeDone,
                            Align = Align.Right | Align.Bottom,
                            Command = new Relay(delegate { Finish(false); })
                        }
                    }
                }
            };
            _window.Closed += delegate { Finish(true); };
        }

        /// <summary>The panel the length is given in.</summary>
        public DurationPanel Panel => _panel;

        /// <summary>Whether it is still waiting for its answer.</summary>
        public bool IsOpen => !_done;

        /// <summary>Hangs the panel from its chip, once the chip is on a screen; nothing where it hangs already or is answered.</summary>
        public void Hang()
        {
            if (_done || _window.IsOpen)
                return;

            var chip = _chip();
            if (chip == null || chip.Windows == null)
                return;

            _window.Anchor = chip;
            _window.Open();
            if (_editor.Input != null)
                _editor.Input.Focus(_panel.Box);

            _panel.Box.SelectAll();
        }

        /// <summary>Puts the panel away and answers with what it says, or that it was put away where <paramref name="cancelled" />.</summary>
        /// <remarks>Once, however it closes: putting the window away comes back here and finds it answered.</remarks>
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
