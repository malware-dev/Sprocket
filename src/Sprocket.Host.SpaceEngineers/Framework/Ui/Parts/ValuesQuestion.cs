using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>The values a part of a sentence says, given in a panel hung from that part whenever it is on a screen.</summary>
    /// <remarks>
    /// Answered once, as it closes: Done or Enter answers with what the panel says once every value takes what it holds, and
    /// Escape or a press anywhere else answers that it was put away.
    /// </remarks>
    public class ValuesQuestion
    {
        readonly SprocketEditor _editor;
        readonly FieldSet _form;
        readonly Label _why;
        readonly Popup _window;
        readonly Action<string, bool> _answered;

        bool _done;

        /// <summary>
        /// A panel asking for <paramref name="fields" />, opening on <paramref name="given" />, hung from what
        /// <paramref name="part" /> finds, and <paramref name="answered" /> with the values it closes on, written as a line of
        /// values, and whether it was put away.
        /// </summary>
        public ValuesQuestion(SprocketEditor editor, IList<Field> fields, IList<Literal> given, Func<Element> part, Action<string, bool> answered)
        {
            _editor = editor;
            _answered = answered;

            var texts = editor.Texts;
            var filled = new List<string>();
            ColorChannels.Shown(given, fields, filled);
            _form = new FieldSet();
            _form.Show(fields, filled, Offered(editor, fields), texts);
            _form.Submitted += Done;
            _form.Edited += () => Explain(_form.Wrong);

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
                        new Label { Style = Styles.Refusal, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _why),
                        new Button
                        {
                            Style = Styles.Primary,
                            Text = texts.ChromeDone,
                            Align = Align.Right,
                            Command = new RelayCommand(Done)
                        }
                    }
                }
            };
            _window.FnAnchor = part;
            _window.Closed += reason => Finish(reason != CloseReason.Accepted);
        }

        /// <summary>
        /// Whether any of <paramref name="fields" /> is given on a screen of its own: slots, chosen on a selector, or text written
        /// over several lines. Values like those are given in the selector, not in a panel.
        /// </summary>
        public static bool Chooses(IList<Field> fields)
        {
            foreach (var field in fields)
            {
                if (field.TakesSlots || field.Edits == FieldEditor.Lines)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Where <paramref name="fields" /> ask for one value, chosen among words, offers those words in a menu under the part
        /// pressed, the one <paramref name="given" /> holds marked, as a state's values are offered, and answers whether it did.
        /// <paramref name="chosen" /> is given another word chosen, written as a line of values, as the panel would answer.
        /// </summary>
        public static bool Offer(SprocketEditor editor, IList<Field> fields, IList<Literal> given, Action<string> chosen)
        {
            if (fields.Count != 1 || fields[0].Edits != FieldEditor.Chosen || fields[0].Words == null)
                return false;

            var field = fields[0];
            var filled = new List<string>();
            ColorChannels.Shown(given, fields, filled);
            var holds = filled.Count == 0 ? "" : field.Shown(filled[0]);
            editor.AskHere("");
            for (var i = 0; i < field.Words.Count; i++)
            {
                var word = field.Words[i];
                var marked = word == holds;
                editor.Answer(word, delegate
                {
                    if (!marked)
                        chosen(field.Meaning(word));
                }, marked);
            }

            return true;
        }

        /// <summary>The form the values are given in.</summary>
        public FieldSet Form => _form;

        /// <summary>Why the values will not do as they stand, or empty.</summary>
        public string Why => _why.Text;

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

        /// <summary>Puts the panel away and answers with what it says, or that it was put away where <paramref name="cancelled" />.</summary>
        /// <remarks>Answers once, however often it is called.</remarks>
        public void Finish(bool cancelled)
        {
            if (_done)
                return;

            _done = true;
            _window.Close();
            _answered(_form.Answer, cancelled);
        }

        /// <summary>Answers with the values, once each holds what it takes; otherwise says why, and gives the keyboard to the first that fails.</summary>
        public void Done()
        {
            _form.Judge();
            Explain(_form.Wrong);
            if (_form.Wrong.Length > 0 || _form.Missing)
            {
                _form.Take(_editor.Input);
                return;
            }

            if (_window.IsOpen)
                _window.Accept();
            else
                Finish(false);
        }

        /// <summary>The colors offered where any of <paramref name="fields" /> asks for one: those the player paints with, only ever read.</summary>
        static ColorOffer Offered(SprocketEditor editor, IList<Field> fields)
        {
            var colors = new List<Hsv>();
            foreach (var field in fields)
            {
                if (field.Edits != FieldEditor.Color)
                    continue;

                Player.Colors(colors);
                break;
            }

            return new ColorOffer(colors, editor.Viewer, editor.Preferred);
        }

        void Explain(string why)
        {
            _why.Text = why;
            _why.IsVisible = why.Length != 0;
        }
    }
}
